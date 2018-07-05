﻿using System;

namespace Ark.Tools.Core.EntityTag
{
    public class EntityTagMismatchException : Exception
    {
        public EntityTagMismatchException(string message)
            : base(message)
        {
        }

        public EntityTagMismatchException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }


        public EntityTagMismatchException(Exception inner, string message)
            : base(message, inner)
        {
        }

        public EntityTagMismatchException(Exception inner, string format, params object[] args)
            : base(string.Format(format, args), inner)
        {
        }

    }
}
