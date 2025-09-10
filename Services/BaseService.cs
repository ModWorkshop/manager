using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Services
{
    public abstract class BaseService
    {
        public virtual void Setup()
        {
            // Override in derived classes if needed
        }
    }
}
