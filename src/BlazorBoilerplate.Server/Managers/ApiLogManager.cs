﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BlazorBoilerplate.Server.Middleware.Wrappers;
using BlazorBoilerplate.Shared.Dto;
using BlazorBoilerplate.Shared.DataInterfaces;
using BlazorBoilerplate.Shared.DataModels;
using BlazorBoilerplate.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlazorBoilerplate.Server.Managers
{
    public class ApiLogManager : IApiLogManager
    {
        private readonly ApplicationDbContext _db;
        private readonly DbContextOptionsBuilder<ApplicationDbContext> _optionsBuilder;
        private readonly IMapper _autoMapper;
        private readonly IUserSession _userSession;

        public ApiLogManager(IConfiguration configuration, ApplicationDbContext db, IMapper autoMapper, IUserSession userSession)
        {
            _db = db;
            _autoMapper = autoMapper;
            _userSession = userSession;

            // Calling Log from the API Middlware results in a disposed ApplicationDBContext. This is here to build a DB Context for logging API Calls
            // If you have a better solution please let me know.
            _optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            if (Convert.ToBoolean(configuration["BlazorBoilerplate:UsePostgresServer"] ?? "false"))
            {
                _optionsBuilder.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
            }
            else if (Convert.ToBoolean(configuration["BlazorBoilerplate:UseSqlServer"] ?? "false"))
            {
                _optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection")); //SQL Server Database
            }
            else
            {
                _optionsBuilder.UseSqlite($"Filename={configuration.GetConnectionString("SqlLiteConnectionFileName")}");  // Sql Lite / file database
            }
        }

        public async Task Log(ApiLogItem apiLogItem)
        {
            if (apiLogItem.ApplicationUserId != Guid.Empty)
            {
                //TODO populate _userSession??

                //var currentUser = _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                //UserSession userSession = new UserSession();
                //if (currentUser != null)
                //{
                //    userSession = new UserSession(currentUser.Result);
                //}
            } 
            else
            {
                apiLogItem.ApplicationUserId = null;
            }

            using (ApplicationDbContext _dbContext = new ApplicationDbContext(_optionsBuilder.Options, _userSession))
            {
                _dbContext.ApiLogs.Add(apiLogItem);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<ApiResponse> Get()
        {
            return new ApiResponse(200, "Retrieved Api Log", _autoMapper.ProjectTo<ApiLogItemDto>(_db.ApiLogs));
        }

        public async Task<ApiResponse> GetByApplicationUserId(Guid applicationUserId)
        {
            try
            {
                return new ApiResponse(200, "Retrieved Api Log", _autoMapper.ProjectTo<ApiLogItemDto>(_db.ApiLogs.Where(a => a.ApplicationUserId == applicationUserId)));
            }
            catch (Exception ex)
            {
                return new ApiResponse(400, ex.Message);
            }
        }
    }
}