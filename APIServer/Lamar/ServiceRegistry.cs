using Lamar;

namespace APIServer.Lamar
{
    public class ServicesRegistry : ServiceRegistry
    {
        public ServicesRegistry()
        {
            Scan(a =>
            {

                a.Assembly("Business");

                a.Include(t => t.Name.EndsWith("Service"));

                a.WithDefaultConventions();
            });
        }
    }
}
