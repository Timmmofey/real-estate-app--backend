//using MaxMind.GeoIP2;
//using MaxMind.GeoIP2.Responses;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;

//namespace AuthService.Infrastructure.IpGeoService
//{
//    public sealed class IpGeoService : IIpGeoService, IDisposable
//    {
//        private readonly DatabaseReader _reader;


//        public IpGeoService(IConfiguration cfg, IHostEnvironment env)
//        {
//            var rawPath = cfg["IpGeoDbPath"] ?? throw new InvalidOperationException("IpGeoDbPath not configured");
//            var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, rawPath));

//            if (!File.Exists(fullPath))
//                throw new FileNotFoundException("MMDB not found: " + fullPath);

//            _reader = new DatabaseReader(fullPath);
//        }

//        public Task<GeoInfo> LookupAsync(string ip)
//        {
//            if (string.IsNullOrWhiteSpace(ip))
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));

//            if (!IPAddress.TryParse(ip, out var ipAddr))
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));


//            try
//            {
//                CityResponse city = _reader.City(ipAddr);

//                string? countryIso = city?.Country?.IsoCode;
//                string? countryName = city?.Country?.Name;
//                string? region = city?.MostSpecificSubdivision?.Name;
//                string? cityName = city?.City?.Name;
//                double? lat = city?.Location?.Latitude;
//                double? lon = city?.Location?.Longitude;

//                var info = new GeoInfo(countryIso, countryName, region, cityName, lat, lon);


//                return Task.FromResult(info);
//            }
//            catch (Exception ex)
//            {
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));
//            }
//        }

//        public void Dispose()
//        {
//            try { _reader?.Dispose(); } catch { }
//        }
//    }
//}

//using MaxMind.GeoIP2;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;

//namespace AuthService.Infrastructure.IpGeoService
//{
//    public sealed class IpGeoService : IIpGeoService, IDisposable
//    {
//        private readonly DatabaseReader _reader;
//        private readonly IMemoryCache _cache;
//        private readonly MemoryCacheEntryOptions _cacheOptions;

//        public IpGeoService(IConfiguration cfg, IHostEnvironment env)
//        {
//            var rawPath = cfg["IpGeoDbPath"] ?? throw new InvalidOperationException("IpGeoDbPath not configured");
//            var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, rawPath));

//            if (!File.Exists(fullPath))
//                throw new FileNotFoundException("MMDB not found: " + fullPath);

//            _reader = new DatabaseReader(fullPath);

//            _cache = new MemoryCache(new MemoryCacheOptions());
//            _cacheOptions = new MemoryCacheEntryOptions
//            {
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10),
//                Priority = CacheItemPriority.High
//            };
//        }

//        public Task<GeoInfo> LookupAsync(string ip)
//        {
//            if (string.IsNullOrWhiteSpace(ip))
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));

//            if (!IPAddress.TryParse(ip, out var ipAddr))
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));

//            // Фильтруем localhost и приватные сети
//            if (IPAddress.IsLoopback(ipAddr) || ipAddr.IsPrivate())
//                return Task.FromResult(new GeoInfo("LOCAL", "Local Network", null, null, null, null));

//            // Проверка кэша
//            if (_cache.TryGetValue(ip, out GeoInfo cached))
//                return Task.FromResult(cached);

//            GeoInfo info;

//            try
//            {
//                var geo = _reader.City(ipAddr);

//                info = new GeoInfo(
//                    geo?.Country?.IsoCode,
//                    geo?.Country?.Name,
//                    geo?.MostSpecificSubdivision?.Name,
//                    geo?.City?.Name,
//                    geo?.Location?.Latitude,
//                    geo?.Location?.Longitude
//                );
//            }
//            catch
//            {
//                info = new GeoInfo(null, null, null, null, null, null);
//            }

//            _cache.Set(ip, info, _cacheOptions);
//            return Task.FromResult(info);
//        }

//        public void Dispose()
//        {
//            try { _reader?.Dispose(); } catch { }
//            _cache.Dispose();
//        }
//    }

//    public static class IPAddressExtensions
//    {
//        public static bool IsPrivate(this IPAddress ip)
//        {
//            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
//                return false; // IPv6 можно добавить отдельно

//            var bytes = ip.GetAddressBytes();
//            if (bytes[0] == 10) return true; // 10.0.0.0/8
//            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
//            if (bytes[0] == 192 && bytes[1] == 168) return true; // 192.168.0.0/16

//            return false;
//        }
//    }

//}

//using MaxMind.GeoIP2;
//using MaxMind.GeoIP2.Responses;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;

//namespace AuthService.Infrastructure.IpGeoService
//{
//    public sealed class IpGeoService : IIpGeoService, IDisposable
//    {
//        private readonly DatabaseReader _reader;
//        private readonly MemoryCache _cache;

//        public IpGeoService(IConfiguration cfg, IHostEnvironment env)
//        {
//            var rawPath = cfg["IpGeoDbPath"] ?? throw new InvalidOperationException("IpGeoDbPath not configured");
//            var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, rawPath));

//            if (!File.Exists(fullPath))
//                throw new FileNotFoundException("MMDB not found: " + fullPath);

//            _cache = new MemoryCache(new MemoryCacheOptions());
//            _reader = new DatabaseReader(fullPath);
//        }

//        public Task<GeoInfo> LookupAsync(string ip)
//        {
//            if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out var ipAddr))
//                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));

//            // Кэшируем Task, чтобы параллельные запросы одного IP не делали двойной lookup
//            var geoTask = _cache.GetOrCreate(ip, entry =>
//            {
//                entry.SetSize(1); // учитываем размер для SizeLimit
//                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10); // TTL кэша
//                return Task.Run(() => DoLookup(ipAddr));
//            });

//            return geoTask;
//        }

//        private GeoInfo DoLookup(IPAddress ipAddr)
//        {
//            try
//            {
//                CityResponse city = _reader.City(ipAddr);

//                return new GeoInfo(
//                    city?.Country?.IsoCode,
//                    city?.Country?.Name,
//                    city?.MostSpecificSubdivision?.Name,
//                    city?.City?.Name,
//                    city?.Location?.Latitude,
//                    city?.Location?.Longitude
//                );
//            }
//            catch
//            {
//                return new GeoInfo(null, null, null, null, null, null);
//            }
//        }

//        public void Dispose()
//        {
//            _reader?.Dispose();
//            _cache.Dispose();
//        }
//    }
//}

using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;



namespace AuthService.Infrastructure.IpGeoService
{
    public sealed class IpGeoService : IIpGeoService, IDisposable
    {
        private readonly DatabaseReader _reader;
        private readonly IMemoryCache _cache;

        public IpGeoService(IConfiguration cfg, IHostEnvironment env, IMemoryCache cache)
        {
            _cache = cache;

            var rawPath = cfg["IpGeoDbPath"]
                ?? throw new InvalidOperationException("IpGeoDbPath not configured");

            var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, rawPath));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("MMDB not found: " + fullPath);

            _reader = new DatabaseReader(fullPath);
        }

        public Task<GeoInfo> LookupAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out var ipAddr))
                return Task.FromResult(new GeoInfo(null, null, null, null, null, null));

            return _cache.GetOrCreateAsync($"geo:{ip}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                entry.Size = 1;

                return Task.FromResult(DoLookup(ipAddr));
            })!;
        }

        private GeoInfo DoLookup(IPAddress ipAddr)
        {
            try
            {
                CityResponse city = _reader.City(ipAddr);

                return new GeoInfo(
                    city.Country?.IsoCode,
                    city.Country?.Name,
                    city.MostSpecificSubdivision?.Name,
                    city.City?.Name,
                    city.Location?.Latitude,
                    city.Location?.Longitude
                );
            }
            catch (AddressNotFoundException)
            {
                return new GeoInfo(null, null, null, null, null, null);
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}