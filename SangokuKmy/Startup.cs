﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Updates;

namespace SangokuKmy
{
  public class Startup
  {
    private readonly ILogger _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
      this.Configuration = configuration;
      _logger = logger;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
      services.AddCors();
      services.AddScoped<SangokuKmyErrorFilterAttribute>();

      // データベースの設定
      Config.Database.MySqlConnectionString = this.Configuration.GetConnectionString("MySql");
      Config.Database.GcmServerKey = this.Configuration.GetSection("ConnectionStrings")["GcmServerKey"];

      // 管理人の設定
      Config.Admin.Name = this.Configuration.GetSection("AdminSettings")["Name"];
      Config.Admin.AliasId = this.Configuration.GetSection("AdminSettings")["AliasId"];
      Config.Admin.Password = this.Configuration.GetSection("AdminSettings")["Password"];
      Config.Admin.GravatarMailAddressMD5 = this.Configuration.GetSection("AdminSettings")["GravatarMailAddressMD5"];

      // ゲームの設定
      Config.Game.SecretKey = this.Configuration.GetSection("GameSettings")["SecretKey"];
      Config.Game.UploadedIconDirectory = this.Configuration.GetSection("GameSettings")["UploadedIconDirectory"];
      Config.Game.HistoricalUploadedIconDirectory = this.Configuration.GetSection("GameSettings")["HistoricalUploadedIconDirectory"];
      Config.Game.IsGenerateAdminCharacter = this.Configuration.GetSection("GameSettings")["IsGenerateAdminCharacter"]?.ToLower() == "true";

      services.AddDbContext<MainContext>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      // リバースプロキシ対応
      app.UseForwardedHeaders(new ForwardedHeadersOptions
      {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
      });

      app.UseStaticFiles();

      // CORSを有効にする
      app.UseCors(builder => builder
                      .WithOrigins("http://localhost:8080", "http://127.0.0.1:8080", "https://sangoku.kmycode.net", "https://sangokukmy-legacy.netlify.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("WWW-Authenticate"));

      // データベースの初期化
      using (var context = new MainContext())
      {
        context.Database.Migrate();
      }

      // 更新処理を開始する
      MlService.Logger = _logger;
      GameUpdater.BeginUpdate(_logger);
      OnlineService.BeginWatch(_logger);

      app.UseMvc();
    }
  }
}
