namespace DBC_Copyer
{
    #region  using directive

    using System;

    #endregion 

    internal sealed class DBCAttribute
    {
        public String Name { get; set; }

        public String ObjectType { get; set; }

        public String ValueType { get; set; }

        public String Content { get; set; }
    }
}
