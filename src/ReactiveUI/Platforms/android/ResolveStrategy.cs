﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace ReactiveUI
{
    public static partial class ControlFetcherMixin
    {
        /// <summary>
        /// Resolution strategy for bindings.
        /// </summary>
        public enum ResolveStrategy
        {
            /// <summary>
            /// Resolve all properties that use a subclass of View.
            /// </summary>
            Implicit,

            /// <summary>
            /// Resolve only properties with an WireUpResource attribute.
            /// </summary>
            ExplicitOptIn,

            /// <summary>
            /// Resolve all View properties and those that use a subclass of View, except those with an IgnoreResource attribute.
            /// </summary>
            ExplicitOptOut
        }
    }
}
