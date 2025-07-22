using Impl;

using Microsoft.AspNetCore.Mvc;

using Nimozyn;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.ScanNimHandlersAsync();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", ([FromServices] INimBus bus) =>
{
    var res = bus.Run(new TestInput1() { Val = 1 });

    return res;
});

app.Run();
