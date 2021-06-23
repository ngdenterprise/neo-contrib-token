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
using Neo.SmartContract.Native;

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

            engine.ExecuteScript<NeoContributorToken>(c => c.mint("Test Contributor", "Test Description", "https://i.picsum.photos/id/856/500/500.jpg?hmac=BOzGgyuyo7weE0xNPxJ_8cw3I7oWUwIiHRN_Y51EoNs"));
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);

            // since this is the second token minted, it's token ID is the contract script hash, incremented by one and sha256 hashed
            var expectedTokenId = snapshot.CalculateTokenId(1);

            engine.ResultStack.Peek(0).Should().BeEquivalentTo(expectedTokenId.AsSpan());
        }


        [Fact(Skip = "Ignore for now")]  
        public void can_puchase_nep11()
        {
            var settings = chain.GetProtocolSettings();
            var alice = chain.GetDefaultAccount("alice").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            var scriptHash = snapshot.GetContractScriptHash<NeoContributorToken>();
            var tokenId = snapshot.CalculateTokenId();

            using var engine = new TestApplicationEngine(snapshot, settings, alice);

            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.NEO.Hash, "transfer", alice, scriptHash, 10);
            // engine.ExecuteScript<NeoContributorToken>(c => c.ownerOf(tokenId,));
            // engine.State.Should().Be(VMState.HALT);
            // engine.ResultStack.Should().HaveCount(1);
            // engine.ResultStack.Peek(0).Should().BeEquivalentTo(Neo.UInt160.Zero);
        }

        
        [Fact]  
        public void can_get_owner_of()
        {
            var settings = chain.GetProtocolSettings();
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();
            var tokenId = snapshot.CalculateTokenId();

            using var engine = new TestApplicationEngine(snapshot, settings);
            engine.ExecuteScript<NeoContributorToken>(c => c.ownerOf(tokenId));
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeEquivalentTo(Neo.UInt160.Zero);
        }

        [Fact]  
        public void can_iterate_tokens()
        {
            var settings = chain.GetProtocolSettings();

            using var snapshot = fixture.GetSnapshot();
            var expectedTokenId = snapshot.CalculateTokenId();

            using var engine = new TestApplicationEngine(snapshot, settings);
            engine.ExecuteScript<NeoContributorToken>(c => c.tokens());

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            var result1 = engine.ResultStack.Peek(0);
            result1.Should().BeOfType<Neo.VM.Types.InteropInterface>();
            var iterator = result1.GetInterface<Neo.SmartContract.Iterators.IIterator>();
            iterator.Should().NotBeNull();
            var iteratorList = iterator.ToList();
            iteratorList.Count.Should().Be(1);
            iteratorList[0].Should().BeEquivalentTo(expectedTokenId.AsSpan());
        }
    }
}
