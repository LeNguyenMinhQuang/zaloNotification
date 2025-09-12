using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SigmaNotificationBackend.Models;
using Microsoft.EntityFrameworkCore; // Đảm bảo namespace này đúng

namespace SigmaNotificationBackend.Services
{
    public class ZaloService : IZaloService
    {
        private readonly HttpClient _httpClient;
        private readonly ZaloOAuthSettings _settings;
        private readonly ILogger<ZaloService> _logger;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly AppDbContext _dbcontext;





        public ZaloService(HttpClient httpClient,
                           IConfiguration configuration,
                           ILogger<ZaloService> logger,
                           ITokenStorageService tokenStorageService,
                           AppDbContext appDbContext
                           )
        {
            _httpClient = httpClient;
            _logger = logger;
            _tokenStorageService = tokenStorageService; // Gán service
            _dbcontext = appDbContext;
            _settings = new ZaloOAuthSettings();
            configuration.GetSection("ZaloOAuth").Bind(_settings);
        }









    }




}


