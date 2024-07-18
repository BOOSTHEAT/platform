using System;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Assets;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class AssetsTests
    {
        [Test]
        public void get_from_disposable_asset()
        {
            var fooDisposable = new FooDisposable();
            var foo = new Asset(fooDisposable);
            var getFoo = foo.Get<FooDisposable>();
            Check.That(ReferenceEquals(fooDisposable, getFoo)).IsTrue();
        }
        
        [Test]
        [Category("ExcludeFromCI")]
        public void get_after_dispose_from_disposable_asset()
        {
            var fooDisposable = new FooDisposable();
            var fooAsset = new Asset(fooDisposable);
            fooAsset.Dispose();
            Check.ThatCode(() => fooAsset.Get<FooDisposable>()).Throws<ContractException>();
        }

        [Test]
        [Category("ExcludeFromCI")]
        public void get_after_from_non_disposable_asset()
        {
            var fizz = new Fizz();
            var fizzAsset = new Asset(fizz);
            fizzAsset.Dispose();
            Check.ThatCode(() => fizzAsset.Get<FooDisposable>()).Not.Throws<ContractException>();
        }

        
        [Test]
        public void get_assets()
        {
            var fizz = new Fizz();
            var assets = new Assets.Assets(new object[]{new FooDisposable(), fizz});
            var getFizz = assets.Get<Fizz>();
            Check.That(ReferenceEquals(fizz, getFizz)).IsTrue();
        }

        
        [Test]
        public void get_assets_abstract_type()
        {
            var qix = new Qix();
            var assets = new Assets.Assets(new object[]{qix});
            var getQix = assets.Get<IQix>();
            Check.That(ReferenceEquals(qix, getQix)).IsTrue();
        }
        
        [Test]
        public void add_assets()
        {
            var fizz = new Fizz();
            var assets = new Assets.Assets(new object[]{new FooDisposable()});
            assets.Add(fizz);
            var getFizz = assets.Get<Fizz>();
            Check.That(ReferenceEquals(fizz, getFizz)).IsTrue();
        }
        
        [Test]
        [Category("ExcludeFromCI")]
        public void add_assets_that_already_exist()
        {
            var fizz = new Fizz();
            var assets = new Assets.Assets(new object[]{new FooDisposable(), fizz});
            Check.ThatCode(() => assets.Add(fizz)).Throws<ContractException>();
        }

        
        [Test]
        [Category("ExcludeFromCI")]
        public void dispose_all_assets()
        {
            var assets = new Assets.Assets(new object[]{new FooDisposable(), new BarDisposable()});
            assets.DisposeAll();
            Check.That(assets.All(a => a.IsDisposed)).IsTrue();
        }
        
        public class FooDisposable : IDisposable
        {
            public void Dispose()
            {
            
            }
        }
        
        public class BarDisposable : IDisposable
        {
            public void Dispose()
            {
            
            }
        }

        public class Fizz
        {
            public Fizz()
            {
            
            }
        }

        public class Qix : IQix
        {
            public Qix()
            {
            }
        }
        public interface IQix
        {
        }
    }

    
}