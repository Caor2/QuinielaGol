using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Quiniela.Startup))]
namespace Quiniela
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
