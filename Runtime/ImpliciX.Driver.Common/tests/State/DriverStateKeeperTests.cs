using System.Collections.Generic;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Driver.Common.Tests.State
{
    [TestFixture]
    public class StateKeeperTests
    {
        [Test]
        public void read_state_for_an_urn_when_it_matches_exactly_the_state_id()
        {
            var stateAb = new DriverState(Urn.BuildUrn("a", "b")).WithValue("a", 42).WithValue("b", 12);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>() {{stateAb.Id, stateAb}});

            var result = sut.TryRead(stateAb.Id);

            result.CheckIsSuccessAnd(actualState => Check.That<IDriverState>(actualState).IsEqualTo(stateAb));
        }

        [Test]
        public void read_state_for_an_urn_when_it_matches_the_state_id()
        {
            var stateAb = new DriverState(Urn.BuildUrn("a", "b")).WithValue("a", 42).WithValue("b", 12);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>() {{stateAb.Id, stateAb}});
            var result = sut.TryRead(Urn.BuildUrn("a", "b", "c"));
            result.CheckIsSuccessAnd(actualState => Check.That(actualState).IsEqualTo(stateAb));
        }

        [Test]
        public void read_state_for_an_urn_when_it_doesnt_matches_the_state_id()
        {
            var state = new DriverState(Urn.BuildUrn("a", "b", "c")).WithValue("a", 42).WithValue("b", 12).WithValue("c",3);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>() {{state.Id, state}});

            var result = sut.TryRead(Urn.BuildUrn("a", "b"));

            result.CheckIsSuccessAnd(actualState => Check.That<bool>(actualState.IsEmpty).IsTrue());
        }

        [Test]
        public void read_state_when_in_all_state_urn_one_matches_exactly_the_state_id()
        {
            var stateAb = new DriverState(Urn.BuildUrn("a", "b")).WithValue("a", 24).WithValue("b", 6);
            var stateAbc = new DriverState(Urn.BuildUrn("a", "b", "c")).WithValue("a", 42).WithValue("b", 12).WithValue("c",3);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>()
            {
                {stateAb.Id, stateAb},
                {stateAbc.Id, stateAbc},
            });

            var result = sut.TryRead(stateAbc.Id);

            result.CheckIsSuccessAnd(actualState => Check.That(actualState).IsEqualTo(stateAbc));
        }

        [Test]
        public void read_state_when_all_state_urn_matches_the_state_id()
        {
            var stateAb = new DriverState(Urn.BuildUrn("a", "b")).WithValue("a", 24).WithValue("b", 6);
            var stateAbc = new DriverState(Urn.BuildUrn("a", "b", "c")).WithValue("a", 42).WithValue("b", 12).WithValue("c",3);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>()
            {
                {stateAb.Id, stateAb},
                {stateAbc.Id, stateAbc},
            });

            var result = sut.TryRead(Urn.BuildUrn("a", "b", "c", "d"));

            result.CheckIsSuccessAnd(actualState => Check.That(actualState).IsEqualTo(stateAbc));
        }

        [Test]
        public void read_state_when_only_one_state_urn_matches_the_state_id()
        {
            var stateAb = new DriverState(Urn.BuildUrn("a", "b")).WithValue("a", 24).WithValue("b", 6);
            var stateAbc = new DriverState(Urn.BuildUrn("a", "b", "c")).WithValue("a", 42).WithValue("b", 12).WithValue("c",3);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>()
            {
                {stateAb.Id, stateAb},
                {stateAbc.Id, stateAbc},
            });
            var result = sut.TryRead(Urn.BuildUrn("a", "b", "d"));
            result.CheckIsSuccessAnd(actualState => Check.That(actualState).IsEqualTo(stateAb));
        }
        
        
        [Test]
        public void read_state_when_state_id_has_the_same_radix_but_is_not_parent_of_searched_urn()
        {
            var stateAbcef = new DriverState(Urn.BuildUrn("a", "b", "c","e","f")).WithValue("a", 42).WithValue("b", 12).WithValue("c",3);
            var sut = new DriverStateKeeper(new Dictionary<string, IDriverState>()
            {
                {stateAbcef.Id, stateAbcef},
            });
            var result = sut.TryRead(Urn.BuildUrn("a", "b", "c", "d" ,"e", "f"));
            result.CheckIsSuccessAnd(actualState =>
            {
                Check.That(actualState).IsNotEqualTo(stateAbcef);
                Check.That<bool>(actualState.IsEmpty).IsTrue();
            });
        }
    }
}