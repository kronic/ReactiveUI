﻿// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Splat;
using Xunit;

namespace ReactiveUI.Tests
{
    public sealed class DependencyResolverTests : IDisposable
    {
        private readonly IDependencyResolver _resolver;

        public DependencyResolverTests()
        {
            _resolver = new ModernDependencyResolver();
            _resolver.InitializeSplat();
            _resolver.InitializeReactiveUI();
            _resolver.RegisterViewsForViewModels(GetType().Assembly);
        }

        /// <summary>
        /// Gets RegistrationNamespaces.
        /// </summary>
        public static IEnumerable<object[]> NamespacesToRegister =>
            new List<object[]>
            {
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.XamForms } },
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.Winforms } },
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.Wpf } },
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.Uno } },
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.Blazor } },
                new object[] { new[] { DependencyResolverMixins.RegistrationNamespace.Drawing } },
                new object[]
                {
                    new[]
                    {
                        DependencyResolverMixins.RegistrationNamespace.XamForms,
                        DependencyResolverMixins.RegistrationNamespace.Wpf
                    }
                },
                new object[]
                {
                    new[]
                    {
                        DependencyResolverMixins.RegistrationNamespace.Blazor,
                        DependencyResolverMixins.RegistrationNamespace.XamForms,
                        DependencyResolverMixins.RegistrationNamespace.Wpf
                    }
                }
            };

        [Fact]
        public void AllDefaultServicesShouldBeRegistered()
        {
            using (_resolver.WithResolver())
            {
                foreach (var shouldRegistered in GetServicesThatShouldBeRegistered(DependencyResolverMixins.DefaultRegistrationNamespaces))
                {
                    IEnumerable<object> resolvedServices = _resolver.GetServices(shouldRegistered.Key);
                    Assert.Equal(shouldRegistered.Value.Count, resolvedServices.Count());
                    foreach (Type implementationType in shouldRegistered.Value)
                    {
                        resolvedServices
                            .Any(rs => rs.GetType() == implementationType)
                            .ShouldBeTrue();
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(NamespacesToRegister))]
        public void AllDefaultServicesShouldBeRegisteredPerRegistrationNamespace(IEnumerable<DependencyResolverMixins.RegistrationNamespace> namespacesToRegister)
        {
            using (_resolver.WithResolver())
            {
                var namespaces = namespacesToRegister.ToArray();

                DependencyResolverMixins.SetRegistrationNamespaces(namespaces);

                foreach (var shouldRegistered in GetServicesThatShouldBeRegistered(namespaces))
                {
                    IEnumerable<object> resolvedServices = _resolver.GetServices(shouldRegistered.Key);

                    Assert.Equal(shouldRegistered.Value.Count, resolvedServices.Count());
                    foreach (Type implementationType in shouldRegistered.Value)
                    {
                        resolvedServices
                            .Any(rs => rs.GetType() == implementationType)
                            .ShouldBeTrue();
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(NamespacesToRegister))]
        public void RegisteredNamespacesShouldBeRegistered(IEnumerable<DependencyResolverMixins.RegistrationNamespace> namespacesToRegister)
        {
            using (_resolver.WithResolver())
            {
                var namespaces = namespacesToRegister.ToArray();

                DependencyResolverMixins.SetRegistrationNamespaces(namespaces);

                foreach (var shouldRegistered in GetServicesThatShouldBeRegistered(namespaces))
                {
                    IEnumerable<object> resolvedServices = _resolver.GetServices(shouldRegistered.Key);

                    resolvedServices
                        .Select(x => x.GetType()?.AssemblyQualifiedName ?? string.Empty)
                        .Any(registeredType => !string.IsNullOrEmpty(registeredType) && DependencyResolverMixins.DefaultRegistrationNamespaces.Except(namespacesToRegister).All(x => !registeredType.Contains(x.ToString())))
                        .ShouldBeTrue();
                }
            }
        }

        public void Dispose()
        {
            _resolver?.Dispose();
        }

        private static IEnumerable<string> GetServiceRegistrationTypeNames(
            IEnumerable<DependencyResolverMixins.RegistrationNamespace> registrationNamespaces)
        {
            foreach (DependencyResolverMixins.RegistrationNamespace registrationNamespace in registrationNamespaces)
            {
                if (registrationNamespace == DependencyResolverMixins.RegistrationNamespace.Wpf)
                {
                    yield return "ReactiveUI.Wpf.Registrations, ReactiveUI.Wpf";
                }

                if (registrationNamespace == DependencyResolverMixins.RegistrationNamespace.XamForms)
                {
                    yield return "ReactiveUI.XamForms.Registrations, ReactiveUI.XamForms";
                }

                if (registrationNamespace == DependencyResolverMixins.RegistrationNamespace.Winforms)
                {
                    yield return "ReactiveUI.Winforms.Registrations, ReactiveUI.Winforms";
                }
            }
        }

        private static Dictionary<Type, List<Type>> GetServicesThatShouldBeRegistered(IReadOnlyList<DependencyResolverMixins.RegistrationNamespace> onlyNamespaces)
        {
            Dictionary<Type, List<Type>> serviceTypeToImplementationTypes = new Dictionary<Type, List<Type>>();

            new Registrations().Register((factory, serviceType) =>
            {
                if (serviceTypeToImplementationTypes.TryGetValue(serviceType, out var implementationTypes) == false)
                {
                    implementationTypes = new List<Type>();
                    serviceTypeToImplementationTypes.Add(serviceType, implementationTypes);
                }

                implementationTypes.Add(factory().GetType());
            });

            new PlatformRegistrations().Register((factory, serviceType) =>
            {
                if (serviceTypeToImplementationTypes.TryGetValue(serviceType, out var implementationTypes) == false)
                {
                    implementationTypes = new List<Type>();
                    serviceTypeToImplementationTypes.Add(serviceType, implementationTypes);
                }

                implementationTypes.Add(factory().GetType());
            });

            var typeNames = GetServiceRegistrationTypeNames(onlyNamespaces);

            typeNames.ForEach(typeName => GetRegistrationsForPlatform(typeName, serviceTypeToImplementationTypes));

            return serviceTypeToImplementationTypes;
        }

        private static void GetRegistrationsForPlatform(string typeName, Dictionary<Type, List<Type>> serviceTypeToImplementationTypes)
        {
            var platformRegistrationsType = Type.GetType(typeName);
            if (platformRegistrationsType != null)
            {
                var platformRegistrations = Activator.CreateInstance(platformRegistrationsType);
                var register = platformRegistrationsType.GetMethod("Register");
                var registerParameter = new Action<Func<object>, Type>((factory, serviceType) =>
                {
                    if (serviceTypeToImplementationTypes.TryGetValue(serviceType, out var implementationTypes) == false)
                    {
                        implementationTypes = new List<Type>();
                        serviceTypeToImplementationTypes.Add(serviceType, implementationTypes);
                    }

                    implementationTypes.Add(factory().GetType());
                });

                register?.Invoke(platformRegistrations, new object[] { registerParameter });
            }
        }
    }
}
