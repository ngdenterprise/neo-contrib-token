# neo-contrib-token - sample Neo NFT token 

## Storage Schema Preview

The `storage-schema-preview` branch has been updated to use the new 
to try out the new Neo Debugger and C# compiler previews.

> Note, the Storage Schema feature is in active development. Please pull changes
  to this branch regularly to ensure you have latest code and tooling available.

* Install the pre-release version of the [Neo Smart Contract Debugger](https://marketplace.visualstudio.com/items?itemName=ngd-seattle.neo-contract-debug).
  You can read about installing pre-release VSCode extensions in the
  [VSCode Release Notes](https://code.visualstudio.com/updates/v1_63#_pre-release-extensions).
  * Pre-release builds of the Neo Smart Contract Debugger have odd minor version numbers.
    Debugger version v3.3 includes support for Storage Schema Preview.
* Clone the `neo-contrib-token` repo and checkout the ``storage-schema-preview`.
* Run the `reset neo express` build task. This task will install the right tools,
  compile the contracts in the repo and create the Neo-Express checkpoints needed
  for all debug launch configurations. Build tasks can be accessed via the VSCode
  `Terminal` menu.
  * This task will install the Storage Schema Preview of the Neo C# Compiler as a 
    [dotnet local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-local-tool).
    Local dotnet tools do don't interfere with global dotnet tools or local dotnet
    tools in other folders.

## NeoContributorToken
1) mint tokens (name, description, image URL) 
2) buy a specified token by transferring 10 NEO to the contract 
3) contract owner can withdraw NEO spent on NFTs 

## DemoShopContract
1) List a token by transferring the NFT to the contract (must specify sale price) 
2) Buy a token by transferring sale price + 1 in NEO to the contract 
3) token owner can cancel a sale before the token has sold 
4) contract owner can withdraw the accumulated +1 NEO fees 

## CLI Client
1) List NFTs 
2) Buy an NFT
