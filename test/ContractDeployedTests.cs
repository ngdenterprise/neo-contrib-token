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

namespace test
{
    [CheckpointPath("checkpoints/contract-deployed.neoxp-checkpoint")]
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
            var logs = new List<string>();
            engine.Log += (sender, args) => { logs.Add(args.Message); };

            engine.ExecuteScript<NeoContributorToken>(c => c.mint("Da Hongfei", "NEO FOUNDER\nFounder of Neo, Chair of Neo Foundation, CEO of NGD", "https://neo3.azureedge.net/images/discover/DaHongfei.jpg"));
            engine.State.Should().Be(VMState.HALT);

        }
    }
}
