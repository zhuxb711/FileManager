﻿using System;
using System.Text;

namespace RX_Explorer.Class
{
    public class OperationListDecompressionModel : OperationListBaseModel
    {
        public override string OperationKindText
        {
            get
            {
                return Globalization.GetString("TaskList_OperationKind_Decompression");
            }
        }

        public Encoding Encoding { get; }

        public bool ShouldCreateFolder { get; }

        public OperationListDecompressionModel(string[] FromPath, string ToPath,bool ShouldCreateFolder, Encoding Encoding = null, EventHandler OnCompleted = null) : base(FromPath, ToPath, OnCompleted)
        {
            this.Encoding = Encoding ?? Encoding.Default;
            this.ShouldCreateFolder = ShouldCreateFolder;
        }
    }
}
