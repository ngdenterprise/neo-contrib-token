# neo-contrib-token - sample Neo NFT token 

## Storage Schema Preview

Please use the
[`storage-schema-preview` branch](https://github.com/ngdenterprise/neo-contrib-token/tree/storage-schema-preview)
to try out the new Neo Debugger and C# compiler previews.

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
