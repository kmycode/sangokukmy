using System;
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

      // ゲームの設定
      Config.Game.UploadedIconDirectory = this.Configuration.GetSection("GameSettings")["UploadedIconDirectory"];

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
                      .WithOrigins("http://localhost:8080", "http://127.0.0.1:8080", "https://sangoku.kmycode.net")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("WWW-Authenticate"));

      // データベースの初期化
      using (var context = new MainContext())
      {
        context.Database.Migrate();
      }

      // 更新処理を開始する
      GameUpdater.BeginUpdate(_logger);
      OnlineService.BeginWatch(_logger);

      app.UseMvc();
    }
  }
}
