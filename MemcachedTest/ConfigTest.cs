using System;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using NUnit.Framework;
using System.Collections.Generic;
using System.Configuration;
using Membase.Configuration;

namespace MemcachedTest
{
	[TestFixture]
	public class ConfigTest
	{
		[TestFixtureSetUp]
		public void Setup()
		{
			log4net.Config.XmlConfigurator.Configure();
		}

		[TestCase]
		public void NewProvidersConfigurationTest()
		{
			ValidateConfig(ConfigurationManager.GetSection("test/newProviders") as IMemcachedClientConfiguration);
		}

		[TestCase]
		public void NewProvidersWithFactoryConfigurationTest()
		{
			ValidateConfig(ConfigurationManager.GetSection("test/newProvidersWithFactory") as IMemcachedClientConfiguration);
		}

		private static void ValidateConfig(IMemcachedClientConfiguration config)
		{
			Assert.IsNotNull(config);

			Assert.IsInstanceOf(typeof(TestKeyTransformer), config.CreateKeyTransformer());
			Assert.IsInstanceOf(typeof(TestLocator), config.CreateNodeLocator());
			Assert.IsInstanceOf(typeof(TestTranscoder), config.CreateTranscoder());
		}

		[TestCase]
		public void TestVBucketConfig()
		{
			IMemcachedClientConfiguration config = ConfigurationManager.GetSection("test/vbucket") as IMemcachedClientConfiguration;
			var loc = config.CreateNodeLocator();
		}

		/// <summary>
		/// Tests if the client can initialize itself from enyim.com/memcached
		/// </summary>
		[TestCase]
		public void DefaultConfigurationTest()
		{
			using (new MemcachedClient()) ;
		}

		/// <summary>
		/// Tests if the client can initialize itself from a specific config
		/// </summary>
		[TestCase]
		public void NamedConfigurationTest()
		{
			using (new MemcachedClient("test/validConfig")) ;
		}

		/// <summary>
		/// Tests if the client can handle an invalid configuration
		/// </summary>
		[TestCase]
		public void InvalidSectionTest()
		{
			try
			{
				using (var client = new MemcachedClient("test/invalidConfig"))
				{
					Assert.IsFalse(false, ".ctor should have failed.");
				}
			}
			catch
			{
				Assert.IsTrue(true);
			}
		}

		[TestCase]
		public void NullConfigurationTest()
		{
			try
			{
				using (var client = new MemcachedClient((IMemcachedClientConfiguration)null))
				{
					Assert.IsFalse(false, ".ctor should have failed.");
				}
			}
			catch
			{
				Assert.IsTrue(true);
			}
		}

		/// <summary>
		/// Tests if the client can be decleratively initialized
		/// </summary>
		[TestCase]
		public void ProgrammaticConfigurationTest()
		{
			// try to hit all lines in the config classes
			MemcachedClientConfiguration mcc = new MemcachedClientConfiguration();

			mcc.Servers.Add(new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
			mcc.Servers.Add(new System.Net.IPEndPoint(IPAddress.Loopback, 20002));

			mcc.NodeLocator = typeof(DefaultNodeLocator);
			mcc.KeyTransformer = new SHA1KeyTransformer();
			mcc.Transcoder = new DefaultTranscoder();

			mcc.SocketPool.MinPoolSize = 10;
			mcc.SocketPool.MaxPoolSize = 100;
			mcc.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 10);
			mcc.SocketPool.DeadTimeout = new TimeSpan(0, 0, 30);

			using (new MemcachedClient(mcc)) ;
		}

		[TestCase]
		public void ProgrammaticConfigurationTestWithDefaults()
		{
			MemcachedClientConfiguration mcc = new MemcachedClientConfiguration();

			// only add servers
			mcc.Servers.Add(new System.Net.IPEndPoint(IPAddress.Loopback, 20000));
			mcc.Servers.Add(new System.Net.IPEndPoint(IPAddress.Loopback, 20002));

			using (new MemcachedClient(mcc)) ;
		}

		[TestCase]
		public void TestPerfMonNull()
		{
			Assert.IsNull(((IMemcachedClientConfiguration)ConfigurationManager.GetSection("test/validConfig")).CreatePerformanceMonitor());
			Assert.IsNull(((IMembaseClientConfiguration)ConfigurationManager.GetSection("test/membase")).CreatePerformanceMonitor());

			Assert.IsNull(((IMemcachedClientConfiguration)new Enyim.Caching.Configuration.MemcachedClientConfiguration()).CreatePerformanceMonitor());
			Assert.IsNull(((IMembaseClientConfiguration)new Membase.Configuration.MembaseClientConfiguration()).CreatePerformanceMonitor());
		}

		[TestCase]
		public void TestPerfMonByMembaseFactory()
		{
			var config = ConfigurationManager.GetSection("test/membasePerfMon") as IMembaseClientConfiguration;

			Assert.IsInstanceOf<TestPerfMon>(config.CreatePerformanceMonitor());
		}

		[TestCase]
		public void TestPerfMonByType()
		{
			var config = ConfigurationManager.GetSection("test/memcachedPerfMonWithType") as IMemcachedClientConfiguration;

			Assert.IsInstanceOf<TestPerfMon>(config.CreatePerformanceMonitor());
		}

		[TestCase]
		public void TestPerfMonByFactory()
		{
			var config = ConfigurationManager.GetSection("test/memcachedPerfMonWithFactory") as IMemcachedClientConfiguration;

			Assert.IsInstanceOf<TestPerfMon>(config.CreatePerformanceMonitor());
		}

		[TestCase]
		public void TestThrottlingFailurePolicy()
		{
			var config = ConfigurationManager.GetSection("test/throttlingFailurePolicy") as IMemcachedClientConfiguration;

			var policyFactory = config.SocketPool.FailurePolicyFactory;

			Assert.IsNotNull(policyFactory);
			Assert.IsInstanceOf<ThrottlingFailurePolicyFactory>(policyFactory);

			var tfp = (ThrottlingFailurePolicyFactory)policyFactory;

			Assert.IsTrue(tfp.FailureThreshold == 10, "failureThreshold must be 10");
			Assert.IsTrue(tfp.ResetAfter == 100, "resetAfter must be 100 msec");
		}
	}

	class TestTranscoderFactory : IProviderFactory<ITranscoder>
	{
		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			Assert.IsTrue(parameters.ContainsKey("test"));
		}

		ITranscoder IProviderFactory<ITranscoder>.Create()
		{
			return new TestTranscoder();
		}
	}

	class TestLocatorFactory : IProviderFactory<IMemcachedNodeLocator>
	{
		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			Assert.IsTrue(parameters.ContainsKey("test"));
		}

		IMemcachedNodeLocator IProviderFactory<IMemcachedNodeLocator>.Create()
		{
			return new TestLocator();
		}
	}

	class TestKeyTransformerFactory : IProviderFactory<IMemcachedKeyTransformer>
	{
		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			Assert.IsTrue(parameters.ContainsKey("test"));
		}

		IMemcachedKeyTransformer IProviderFactory<IMemcachedKeyTransformer>.Create()
		{
			return new TestKeyTransformer();
		}
	}

	class TestTranscoder : ITranscoder
	{
		CacheItem ITranscoder.Serialize(object o)
		{
			return new CacheItem();
		}

		object ITranscoder.Deserialize(CacheItem item)
		{
			return null;
		}
	}

	class TestLocator : IMemcachedNodeLocator
	{
		private IList<IMemcachedNode> nodes;

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			this.nodes = nodes;
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			return null;
		}

		IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
		{
			return this.nodes.ToArray();
		}
	}

	class TestKeyTransformer : IMemcachedKeyTransformer
	{
		string IMemcachedKeyTransformer.Transform(string key)
		{
			return null;
		}
	}

	class TestMembasePerfMonFactory : IMembasePerformanceMonitorFactory
	{
		IPerformanceMonitor IMembasePerformanceMonitorFactory.Create(string bucket)
		{
			return new TestPerfMon();
		}

		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			Assert.IsTrue(parameters.ContainsKey("test"));
		}
	}

	class TestMemcachedPerfMonFactory : IProviderFactory<IPerformanceMonitor>
	{
		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			Assert.IsTrue(parameters.ContainsKey("test"));
		}

		IPerformanceMonitor IProviderFactory<IPerformanceMonitor>.Create()
		{
			return new TestPerfMon();
		}
	}

	class TestPerfMon : IPerformanceMonitor
	{
		#region IPerformanceMonitor Members

		void IPerformanceMonitor.Get(int amount, bool success)
		{
			throw new NotImplementedException();
		}

		void IPerformanceMonitor.Store(StoreMode mode, int amount, bool success)
		{
			throw new NotImplementedException();
		}

		void IPerformanceMonitor.Delete(int amount, bool success)
		{
			throw new NotImplementedException();
		}

		void IPerformanceMonitor.Mutate(MutationMode mode, int amount, bool success)
		{
			throw new NotImplementedException();
		}

		void IPerformanceMonitor.Concatenate(ConcatenationMode mode, int amount, bool success)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
