using System.Text;

namespace MikroApi
{
    internal class MikroTikConnectionSlot : MikroTikConnection
    {
        internal PoolSlot<MikroTikConnectionSlot> PooledObject { get; set; }
        bool disposeInnerObject;

        public MikroTikConnectionSlot(Encoding encoding) : base(encoding)
        {
            
        }

        internal void DisposeInnerObject()
        {
            disposeInnerObject = true;
            Dispose();
        }

        protected override void DisposeManaged()
        {
            if (disposeInnerObject)
            {
                base.DisposeManaged();
            }
            else
            {
                PooledObject.Dispose();
            }
        }
    }
}
