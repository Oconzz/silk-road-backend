using Lamar;

namespace APIServer.Lamar
{
    public class HandlerRegistry : ServiceRegistry
    {
        public HandlerRegistry()
        {
            Scan(a =>
            {

                a.Assembly("Business");

                a.Include(t => t.Name.EndsWith("Handler"));

                a.WithDefaultConventions();
            });
        }
    }
}
