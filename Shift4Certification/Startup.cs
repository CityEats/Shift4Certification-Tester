using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Shift4Certification.Startup))]
namespace Shift4Certification
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
