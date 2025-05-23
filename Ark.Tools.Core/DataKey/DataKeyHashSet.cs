﻿// Copyright (C) 2024 Ark Energy S.r.l. All rights reserved.
// Licensed under the MIT License. See LICENSE file for license information. 
using System.Collections.Generic;

namespace Ark.Tools.Core.DataKey
{
    public class DataKeyHashSet<T> : HashSet<T> where T : class
    {
        public DataKeyHashSet() : base(new DataKeyComparer<T>())
        { }

        public DataKeyHashSet(IEnumerable<T> collection) : base(collection, new DataKeyComparer<T>())
        { }
    }
}
