using MEDv1.Data;
using MEDv1.Abstractions;
using ChecklistApp.Services;
using MEDv1.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<SqlConnectionFactory>();

builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IReportResultRepository, ReportResultRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<IEntryFormService, EntryFormService>();
builder.Services.AddScoped<IRequirementService, RequirementService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<MERTransactionService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Entry}/{action=Lookup}/{id?}");

app.MapGet("/", () => Results.Redirect("/Entry/Lookup"));


app.Run();
