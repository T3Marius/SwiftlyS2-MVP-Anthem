using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Services;

namespace MVP_Anthem;

public class DatabaseManager
{
    private readonly ISwiftlyCore _core;
    private readonly Dictionary<int, PlayerMvp> _mvps = new();

    public DatabaseManager(ISwiftlyCore core)
    {
        _core = core;
    }

    private IDbConnection GetConnection()
    {
        try
        {
            var dbConn = _core.Database.GetConnection("mvp-anthem");
            dbConn.Open();
            return dbConn;
        }
        catch
        {
            throw;
        }
    }

    public async Task CreateTable()
    {
        try
        {
            using var conn = GetConnection();

            // Detect if this is SQLite or MySQL/MariaDB
            bool isSQLite = conn.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

            string createTableQuery = isSQLite
                ? @"
                    CREATE TABLE IF NOT EXISTS player_mvp_settings (
                        steamid TEXT NOT NULL PRIMARY KEY,
                        mvpname TEXT NOT NULL,
                        mvpsound TEXT NOT NULL,
                        volume REAL NOT NULL DEFAULT 1.0
                    );
                  "
                : @"
                    CREATE TABLE IF NOT EXISTS `player_mvp_settings` (
                        `steamid` VARCHAR(255) NOT NULL PRIMARY KEY,
                        `mvpname` VARCHAR(255) NOT NULL,
                        `mvpsound` VARCHAR(255) NOT NULL,
                        `volume` FLOAT NOT NULL DEFAULT 1.0
                    );
                  ";

            int result = await conn.ExecuteAsync(createTableQuery);
        }
        catch
        {
            throw;
        }
    }

    public async Task SaveMvp(IPlayer player, string? mvpname, string? mvpsound, float? volume = null)
    {
        if (player == null)
        {
            return;
        }

        try
        {
            using var conn = GetConnection();
            bool isSQLite = conn.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

            string ensureQuery = isSQLite
                ? @"
                    INSERT INTO player_mvp_settings (steamid, mvpname, mvpsound, volume)
                    VALUES (@SteamId, '', '', 1.0)
                    ON CONFLICT(steamid) DO NOTHING;
                  "
                : @"
                    INSERT INTO player_mvp_settings (steamid, mvpname, mvpsound, volume)
                    VALUES (@SteamId, '', '', 1.0)
                    ON DUPLICATE KEY UPDATE steamid = steamid;
                  ";

            await conn.ExecuteAsync(ensureQuery, new { SteamId = player.SteamID });

            List<string> updates = new();
            if (mvpname != null) updates.Add("mvpname = @MvpName");
            if (mvpsound != null) updates.Add("mvpsound = @MvpSound");
            if (volume.HasValue) updates.Add("volume = @Volume");

            if (updates.Count == 0)
            {
                return;
            }

            string query = $@"
                UPDATE player_mvp_settings
                SET {string.Join(", ", updates)}
                WHERE steamid = @SteamId;
            ";

            var parameters = new
            {
                SteamId = player.SteamID,
                MvpName = mvpname,
                MvpSound = mvpsound,
                Volume = volume
            };

            int result = await conn.ExecuteAsync(query, parameters);
        }
        catch
        {
        }
    }
    public PlayerMvp? GetMvp(IPlayer player)
    {
        return GetMvpAsync(player).GetAwaiter().GetResult();
    }
    private async Task<PlayerMvp?> GetMvpAsync(IPlayer player)
    {
        if (player == null)
        {
            return null;
        }

        try
        {
            using var conn = GetConnection();

            string query = @"
                SELECT mvpname AS MVPName,
                       mvpsound AS MVPSound,
                       volume
                FROM player_mvp_settings
                WHERE steamid = @SteamId
                LIMIT 1;
            ";

            var result = await conn.QueryFirstOrDefaultAsync<PlayerMvp>(query, new { SteamId = player.SteamID });

            if (result == null)
            {
                return null;
            }

            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task RemoveMvp(IPlayer player)
    {
        if (player == null)
        {
            return;
        }

        try
        {
            using var conn = GetConnection();

            string query = @"
                UPDATE player_mvp_settings
                SET 
                    mvpname = '',
                    mvpsound = ''
                WHERE steamid = @SteamId;
            ";

            int result = await conn.ExecuteAsync(query, new { SteamId = player.SteamID });

            if (_mvps.ContainsKey(player.PlayerID))
            {
                var current = _mvps[player.PlayerID];
                current.MVPName = string.Empty;
                current.MVPSound = string.Empty;
                _mvps[player.PlayerID] = current;
            }
        }
        catch
        {
        }
    }
    public async Task<PlayerMvp?> LoadMvp(IPlayer player, float defaultVolume)
    {
        if (player == null)
        {
            return null;
        }

        try
        {
            using var conn = GetConnection();

            string checkQuery = @"
            SELECT COUNT(*) 
            FROM player_mvp_settings
            WHERE steamid = @SteamId;
        ";

            int count = await conn.ExecuteScalarAsync<int>(checkQuery, new { SteamId = player.SteamID });

            if (count == 0)
            {
                string insertQuery = @"
                INSERT INTO player_mvp_settings (steamid, mvpname, mvpsound, volume)
                VALUES (@SteamId, '', '', @Volume);
            ";

                await conn.ExecuteAsync(insertQuery, new
                {
                    SteamId = player.SteamID,
                    Volume = defaultVolume
                });
            }

            string selectQuery = @"
                SELECT mvpname AS MVPName,
                    mvpsound AS MVPSound,
                    volume
                FROM player_mvp_settings
                WHERE steamid = @SteamId
                LIMIT 1;
            ";

            var mvp = await conn.QueryFirstOrDefaultAsync<PlayerMvp>(selectQuery, new { SteamId = player.SteamID });

            if (mvp == null)
            {
                return new PlayerMvp { MVPName = "", MVPSound = "", Volume = defaultVolume };
            }

            _mvps[player.PlayerID] = mvp;

            return mvp;
        }
        catch
        {
            throw;
        }
    }

}
