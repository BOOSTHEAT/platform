using System.Reactive.Subjects;
using DynamicData;
using ImpliciX.Data.Factory;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;
using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;

public class SessionServiceTests
{
  [Test]
  public void datastore_properties_are_sync_with_received_properties()
  {
    var concierge = ILightConcierge.Create();

    concierge.RemoteDevice.Properties.AddOrUpdate(new ImpliciXProperty("root:prop1", "0.1"));
    concierge.RemoteDevice.Properties.AddOrUpdate(new ImpliciXProperty("root:prop2", "0.2"));

    concierge.Session.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();

    Check.That(storeProperties).ContainsExactly(new[]
    {
      new ImpliciXProperty("root:prop1", "0.1"),
      new ImpliciXProperty("root:prop2", "0.2")
    });

    concierge.RemoteDevice.Properties.AddOrUpdate(new ImpliciXProperty("root:prop1", "0.3"));

    Check.That(storeProperties).IsEquivalentTo(new[]
    {
      new ImpliciXProperty("root:prop1", "0.3"),
      new ImpliciXProperty("root:prop2", "0.2")
    });

    concierge.RemoteDevice.Properties.Remove(new ImpliciXProperty("root:prop2", "0.2"));

    Check.That(storeProperties).ContainsExactly(new[]
    {
      new ImpliciXProperty("root:prop1", "0.3")
    });
  }

  [Test]
  public void when_application_is_loaded_enum_properties_are_transformed()
  {
    var mf = new ModelFactory(this.GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });


    var projectsManager = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());

    var appConnection = MockAppConnection();

    var sut = new SessionService(appConnection, projectsManager.Devices);
    sut.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "-1"));
    sut.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "25.2"));
    sut.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));

    projectsManager.OnMake(deviceDefinition.Object);

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();

    Check.That(storeProperties).ContainsExactly(new[]
    {
      new ImpliciXProperty("root:dummy", "Foo"),
      new ImpliciXProperty("root:t1", "25.2"),
      new ImpliciXProperty("root:state", "A"),
    });
  }

  [Test]
  public void when_deviceDefinition_from_project_is_known()
  {
    var mf = new ModelFactory(this.GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();

    var projectsManager = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());
    var sut = new SessionService(appConnection, projectsManager.Devices);

    projectsManager.OnMake(deviceDefinition.Object);

    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "0"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "0.2"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();

    Check.That(storeProperties).ContainsExactly(new[]
    {
      new ImpliciXProperty("root:dummy", "Bar"),
      new ImpliciXProperty("root:t1", "0.2"),
      new ImpliciXProperty("root:state", "A")
    });
  }

  [Test]
  public void when_project_is_unloaded_properties_are_cleared()
  {
    var mf = new ModelFactory(this.GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();

    var projectsManager = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());
    var sut = new SessionService(appConnection, projectsManager.Devices);

    projectsManager.OnMake(deviceDefinition.Object);

    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "0"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "0.2"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();
    
    projectsManager.UnLoad();

    Check.That(storeProperties).IsEmpty();
  }

  [Test]
  public void when_application_is_loaded_properties_are_known()
  {
    var mf = new ModelFactory(this.GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();
    var appManager = MockApplicationsManager(deviceDefinition.Object);
    var sut = new SessionService(appConnection, appManager.Devices);
    
    appManager.Load("whatever");

    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "0"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "0.2"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();
    
    Check.That(storeProperties).ContainsExactly(new[]
    {
      new ImpliciXProperty("root:dummy", "Bar"),
      new ImpliciXProperty("root:t1", "0.2"),
      new ImpliciXProperty("root:state", "A")
    });
  }

  [Test]
  public void when_application_is_unloaded_properties_are_cleared()
  {
    var mf = new ModelFactory(this.GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();
    var appManager = MockApplicationsManager(deviceDefinition.Object);
    var sut = new SessionService(appConnection, appManager.Devices);
    
    appManager.Load("whatever");

    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "0"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "0.2"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();
    
    appManager.UnLoad();

    Check.That(storeProperties).IsEmpty();
  }

  [Test]
  public void GivenUrnIsUnknownInDeviceDefinition_ThenUrnPropertyShouldBeAddedToTheStore()
  {
    var mf = new ModelFactory(GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();

    var projectsManager = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());
    var sut = new SessionService(appConnection, projectsManager.Devices);

    projectsManager.OnMake(deviceDefinition.Object);

    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:dummy", "0"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "0.2"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:state", "1"));
    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("urn:unknown:in:device:definition", "123"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();

    Check.That(storeProperties).ContainsExactly
    (
      new ImpliciXProperty("root:dummy", "Bar"),
      new ImpliciXProperty("root:t1", "0.2"),
      new ImpliciXProperty("root:state", "A"),
      new ImpliciXProperty("urn:unknown:in:device:definition", "123")
    );
  }

  [Test]
  public void
    GivenUrnIsKnownInDeviceDefinition_WhenPropertyStringValueDoesNotFitWithAssemblyValueType_ThenThatPropertyNotificationIsSkipped()
  {
    var mf = new ModelFactory(GetType().Assembly);

    var subSystemDefinition = new Mock<ISubSystemDefinition>();
    subSystemDefinition.Setup(o => o.ID).Returns("root");
    subSystemDefinition.Setup(o => o.StateUrn).Returns(root.state);
    subSystemDefinition.Setup(o => o.StateType).Returns(typeof(DummyState));

    var deviceDefinition = new Mock<IDeviceDefinition>();
    deviceDefinition.Setup(o => o.ModelFactory).Returns(mf);
    deviceDefinition.Setup(o => o.Urns).Returns(mf.GetAllUrns().ToDictionary(u => u.Value, u => u));
    deviceDefinition.Setup(o => o.SubSystemDefinitions).Returns(new[] { subSystemDefinition.Object });

    var appConnection = MockAppConnection();

    var projectsManager = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());
    var sut = new SessionService(appConnection, projectsManager.Devices);

    projectsManager.OnMake(deviceDefinition.Object);


    appConnection.Properties.AddOrUpdate(new ImpliciXProperty("root:t1", "WrongFloatValue"));

    sut.Properties.Connect()
      .Transform(it => it)
      .Bind(out var storeProperties)
      .Subscribe();

    Check.That(storeProperties).IsEmpty();
  }

  [Test]
  public void SessionsAreObservable()
  {
    var appConnection = MockAppConnection();
    var devices = new Subject<Option<IDeviceDefinition>>();
    ISessionService sut = new SessionService(appConnection, devices);
    var sessions = new List<ISessionService.Session>();
    sut.Updates.Subscribe(s => sessions.Add(s));

    devices.OnNext(MockDeviceDefinition("/foo", "foo", "1.0"));
    ((Subject<ITargetSystem>)appConnection.TargetSystem).OnNext(MockTargetSystem("bar"));

    Assert.That(sut.Current, Is.EqualTo(new ISessionService.Session("/foo", "bar")));
    Assert.That(sessions, Is.EqualTo(new[]
    {
      new ISessionService.Session("/foo", ""),
      new ISessionService.Session("/foo", "bar"),
    }));
  }

  [TestCase("", "", "", "", false)]
  [TestCase("/foo", "foo", "", "", true)]
  [TestCase("/bar", "bar", "", "", true)]
  [TestCase("", "", "foo", "", true)]
  [TestCase("", "", "bar", "", true)]
  [TestCase("", "", "bar", "qix", true)]
  [TestCase("/foo", "foo", "bar", "foo", true)]
  [TestCase("/foo", "foo", "bar", "qix", false)]
  public void WorthySession(string path, string localName, string connection, string remoteName, bool worthy)
  {
    var session = new SessionDetails(
      path,
      new SessionDetails.AppIdentity(localName, ""),
      connection,
      new SessionDetails.AppIdentity(remoteName, ""));
    Assert.That(session.IsWorthy, Is.EqualTo(worthy));
  }

  [Test]
  public void SessionsHaveHistory()
  {
    UserSettings.Clear(SessionService.PersistenceKey);
    var appConnection = MockAppConnection();
    var devices = new Subject<Option<IDeviceDefinition>>();
    ISessionService sut = new SessionService(appConnection, devices);
    var sessions = new List<ISessionService.Session[]>();
    sut.HistoryUpdates.Subscribe(s => sessions.Add(s.ToArray()));

    devices.OnNext(MockDeviceDefinition("/foo", "foo", "1.0"));
    ((Subject<ITargetSystem>)appConnection.TargetSystem).OnNext(MockTargetSystem("bar"));

    Assert.That(sut.History, Is.EqualTo(new[]
    {
      new ISessionService.Session("/foo", "bar"),
      new ISessionService.Session("/foo", ""),
    }));
    Assert.That(sessions, Is.EqualTo(new[]
    {
      new[]
      {
        new ISessionService.Session("/foo", ""),
      },
      new[]
      {
        new ISessionService.Session("/foo", "bar"),
        new ISessionService.Session("/foo", ""),
      },
    }));
  }

  private static IRemoteDevice MockAppConnection()
  {
    var appConnectionProperties = new SourceCache<ImpliciXProperty, string>(it => it.Urn);
    var appConnection = new Mock<IRemoteDevice>();
    appConnection.Setup(it => it.Properties).Returns(appConnectionProperties);
    appConnection.Setup(x => x.TargetSystem).Returns(new Subject<ITargetSystem>());
    appConnection.Setup(x => x.DeviceDefinition).Returns(new Subject<IRemoteDeviceDefinition>());
    return appConnection.Object;
  }

  private static Option<IDeviceDefinition> MockDeviceDefinition(string path, string name, string version)
  {
    var dd = new Mock<IDeviceDefinition>();
    dd.Setup(x => x.Path).Returns(path);
    dd.Setup(x => x.Name).Returns(name);
    dd.Setup(x => x.Version).Returns(version);
    dd.Setup(x => x.Urns).Returns(new Dictionary<string, Urn>());
    return Option<IDeviceDefinition>.Some(dd.Object);
  }

  private static ITargetSystem MockTargetSystem(string connectionString)
  {
    var ts = new Mock<ITargetSystem>();
    ts.Setup(x => x.ConnectionString).Returns(connectionString);
    return ts.Object;
  }
  
  
  private static ApplicationsManager MockApplicationsManager(IDeviceDefinition dd)
  {
    var definitionFactory = new Mock<IDeviceDefinitionFactory>();
    definitionFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<ApplicationDefinition>()))
      .Returns<string,ApplicationDefinition>((path,appDef) => dd);
    var appManager = new ApplicationsManager(
      Mock.Of<IConsoleService>(),
      x => new ApplicationDefinition { AppName = $"App[{x}]" },
      definitionFactory.Object
    );
    return appManager;
  }

}