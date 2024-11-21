using Big_Project_v3.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 添加服務到 DI 容器
builder.Services.AddDbContext<ITableDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("iTableDBConnection"));
});
// 設定使用 SQL Server 的 iTableDbContext，連接字串從 appsettings.json 中的 iTableDBConnection 讀取

builder.Services.AddControllersWithViews(); // 註冊 MVC 服務

// 配置 Session 支援
builder.Services.AddDistributedMemoryCache(); // 添加內存緩存（Session 的基礎存儲）
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 設置 Session 的過期時間
    options.Cookie.HttpOnly = true; // 只能通過 HTTP 訪問 Session
    options.Cookie.IsEssential = true; // 標記為必需的 Cookie
});

builder.Services.AddHttpContextAccessor(); // 添加 HttpContextAccessor 支援，方便在控制器中訪問 HttpContext

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

// 啟用 Session 中介軟體
app.UseSession(); // 必須在 app.UseAuthorization() 之前

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Member}/{action=Index}/{id?}");

app.Run();
