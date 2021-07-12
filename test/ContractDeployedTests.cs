using System;
using Xunit;
using Neo.BlockchainToolkit;
using System.IO.Abstractions;
using Neo.BlockchainToolkit.Models;
using Neo.Persistence;
using Neo;
using Neo.SmartContract.Native;
using Neo.SmartContract;
using Neo.VM;
using System.Linq;
using Neo.SmartContract.Manifest;
using NeoTestHarness;
using FluentAssertions;
using testNeoContributorToken;
using Neo.Assertions;
using Neo.BlockchainToolkit.SmartContract;
using System.Collections.Generic;
using Neo.IO;
using Neo.Cryptography;

namespace test
{
    [CheckpointPath("checkpoints/contracts-deployed.neoxp-checkpoint")]
    public class ContractDeployedTests : IClassFixture<CheckpointFixture<ContractDeployedTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public ContractDeployedTests(CheckpointFixture<ContractDeployedTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain();
        }

        [Fact]
        public void contract_owner_in_storage()
        {
            var settings = chain.GetProtocolSettings();
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            // check to make sure contract owner stored in contract storage
            var storages = snapshot.GetContractStorages<NeoContributorToken>();
            storages.Count().Should().Be(1);
            storages.TryGetValue(Common.CONTRACT_OWNER_KEY, out var item).Should().BeTrue();
            item!.Should().Be(owen);
        }

        [Fact]  
        public void can_mint()
        {
            var settings = chain.GetProtocolSettings();
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();
            using var engine = new TestApplicationEngine(snapshot, settings, owen);

            engine.ExecuteScript<NeoContributorToken>(c => c.mint("Test Contributor", "Test Description", "https://i.picsum.photos/id/856/500/500.jpg?hmac=BOzGgyuyo7weE0xNPxJ_8cw3I7oWUwIiHRN_Y51EoNs", "1"));
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);

            var expectedTokenId = snapshot.CalculateTokenId(0);
            engine.ResultStack.Peek(0).Should().BeEquivalentTo(expectedTokenId.AsSpan());
        }
    }
}
