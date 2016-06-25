﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Syrinj.Scripts.Graph;
using Syrinj.Attributes;
using Syrinj.Caching;
using Syrinj.Exceptions;
using Syrinj.Providers;
using Syrinj.Graph;
using UnityEngine;

namespace Syrinj.Injection
{
    public class MonoBehaviourInjector
    {
        private static IDependencyGraph graph = new DependencyMap();

        private static readonly Dictionary<Type, IProvider> defaultProviders = ProviderGroups.Default;

        private static readonly AttributeCache attributeCache = new AttributeCache();

        private readonly MonoBehaviour _monoBehaviour;
        private readonly Type _derivedType;

        private readonly List<Injectable> _injectables;

        public MonoBehaviourInjector(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
            _derivedType = monoBehaviour.GetType();
            _injectables = new List<Injectable>();
        }

        public void Inject()
        {
            LoadMembers();

            InjectAll();
        }

        private void LoadMembers()
        {
            var allMembers = attributeCache.GetMembersForType(_derivedType);

            for (int i = 0; i < allMembers.Count; i++)
            {
                LoadMemberAttributes(allMembers[i]);
            }
        }

        private void LoadMemberAttributes(MemberInfo info)
        {
            LoadInjectorAttributes(info);
            LoadProviderAttributes(info);
        }

        private void LoadInjectorAttributes(MemberInfo info)
        {
            var attributes = attributeCache.GetInjectorAttributesForMember(info);
            if (attributes == null) return;

            for (int i = 0; i < attributes.Count; i++)
            {
                var injectable = InjectableFactory.Create(info, attributes[i], _monoBehaviour);
                if (injectable != null)
                {
                    _injectables.Add(injectable);
                }
            }
        }

        private void LoadProviderAttributes(MemberInfo info)
        {
            var attributes = attributeCache.GetProviderAttributesForMember(info);
            if (attributes == null) return;

            for (int i = 0; i < attributes.Count; i++)
            {
                //graph.RegisterProvider();
            }
        }

        private void InjectAll()
        {
            for (int i = 0; i < _injectables.Count; i++)
            {
                ResolveDependencyAndInject(_injectables[i]);
            }
        }

        private void ResolveDependencyAndInject(Injectable injectable)
        {
            var provider = defaultProviders[injectable.Attribute.GetType()];

            if (provider != null)
            {
                var dependencyFromProvider = provider.Provide(injectable);
                TryInject(injectable, dependencyFromProvider);
            }
            else
            {
                var dependencyFromGraph = graph.Get(injectable.Type);
                TryInject(injectable, dependencyFromGraph);
            }
        }

        private void TryInject(Injectable injectable, object dependency)
        {
            if (ValidDependency(injectable, dependency))
            {
                throw new InjectionException(_monoBehaviour, "Could not find dependency for " + injectable.Type);
            }
            else
            {
                injectable.Inject(dependency);
            }
        }

        private bool ValidDependency(Injectable injectable, object dependency)
        {
            return dependency == null || dependency.Equals(null) || !injectable.Type.IsInstanceOfType(dependency);
        }
    }
}