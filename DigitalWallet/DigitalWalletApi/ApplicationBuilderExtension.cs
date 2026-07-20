using Hangfire;
using Serilog;

namespace DigitalWalletApi
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication UsePresentation(
            this WebApplication app)
        {
            //app.UseMiddleware<GlobalExceptionHandler>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Digital Wallet API v1");
            });
            app.UseHangfireDashboard();


            app.UseRouting();

            app.UseCors("AllowFrontend");


            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSerilogRequestLogging();
            app.MapControllers().RequireCors("AllowFrontend");


            return app;
        }
    }}
