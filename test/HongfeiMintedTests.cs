using System;
using Xunit;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.SmartContract;
using Neo.VM;
using System.Linq;
using NeoTestHarness;
using FluentAssertions;
using testNeoContributorToken;
using Neo.Assertions;
using Neo.BlockchainToolkit.SmartContract;
using System.Collections.Generic;
using Neo.IO;
using Neo.Cryptography;
using System.Numerics;

namespace test
{
    [CheckpointPath("checkpoints/hongfei-token-minted.neoxp-checkpoint")]
    public class HongfeiMintedTests : IClassFixture<CheckpointFixture<HongfeiMintedTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public HongfeiMintedTests(CheckpointFixture<HongfeiMintedTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain();
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

            engine.ExecuteScript<NeoContributorToken>(c => c.mint("Test Contributor", "Test Description", "https://i.picsum.photos/id/856/500/500.jpg?hmac=BOzGgyuyo7weE0xNPxJ_8cw3I7oWUwIiHRN_Y51EoNs"));
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);

            // since this is the second token minted, it's token ID is the contract script hash, incremented by one and sha256 hashed
            var scriptHash = snapshot.GetContractScriptHash<NeoContributorToken>().ToArray();
            var expectedTokenId = (new BigInteger(scriptHash) + 1).ToByteArray().Sha256();

            engine.ResultStack.Peek(0).Should().BeEquivalentTo(expectedTokenId.AsSpan());
        }
        
        [Fact]  
        public void can_get_owner_of()
        {
            var settings = chain.GetProtocolSettings();
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            var scriptHash = snapshot.GetContractScriptHash<NeoContributorToken>();
            var tokenId = scriptHash.ToArray().Sha256();

            using var engine = new TestApplicationEngine(snapshot, settings);
            engine.ExecuteScript<NeoContributorToken>(c => c.ownerOf(tokenId));
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeEquivalentTo(Neo.UInt160.Zero);
        }
    }
}
