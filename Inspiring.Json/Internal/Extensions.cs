using System;
using System.Collections.Generic;
using System.Text;

namespace Inspiring {
    public static class Extensions {
        public static string FormatWith(this string format, params object?[] args)
            => String.Format(format, args);
    }
}
