﻿using System;
using System.Data;

namespace Ark.Tools.Sql
{
    public interface ISqlContext<Tag> : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        void Commit();
        void Rollback();
        void ChangeIsolationLevel(IsolationLevel isolationLevel);

    }
}
