﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RX_Explorer.Class
{
    public sealed class OperationRecorder
    {
        private readonly ConcurrentStack<List<string>> Container;

        private static OperationRecorder Instance;

        public void Push(params string[] InputList)
        {
            if (InputList.Length > 0)
            {
                List<string> FilterList = new List<string>();

                foreach (string Record in InputList)
                {
                    string SourcePath = Record.Split("||").FirstOrDefault();

                    if (FilterList.Select((Rec) => Rec.Split("||").FirstOrDefault()).All((RecPath) => !SourcePath.StartsWith(RecPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        FilterList.Add(Record);
                    }
                }

                if (FilterList.Count > 0)
                {
                    Container.Push(FilterList);
                }
            }
        }

        public List<string> Pop()
        {
            if (Container.TryPop(out List<string> Result))
            {
                return Result;
            }
            else
            {
                return new List<string>(0);
            }
        }

        public bool IsNotEmpty 
        { 
            get => !Container.IsEmpty; 
        }

        public static OperationRecorder Current
        {
            get
            {
                return Instance ??= new OperationRecorder();
            }
        }

        private OperationRecorder()
        {
            Container = new ConcurrentStack<List<string>>();
        }
    }
}
