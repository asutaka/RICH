using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPr.DAL.Entity;

namespace TestPr.DAL
{
    public interface ISignalRepo : IBaseRepo<Signal>
    {
    }

    public class SignalRepo : BaseRepo<Signal>, ISignalRepo
    {
        public SignalRepo()
        {
        }
    }
}
