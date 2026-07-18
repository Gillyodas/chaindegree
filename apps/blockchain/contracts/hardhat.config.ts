import { HardhatUserConfig } from "hardhat/config";
import "@nomicfoundation/hardhat-toolbox";
import * as dotenv from "dotenv";

dotenv.config();

// Standard Besu dev private key (pre-funded in Besu dev network)
const DEV_PRIVATE_KEY = "0x8f2a55949038a9610f50fb23b5883af3b4ecb3c3bb792cbcefbd1542c692be63";

const config: HardhatUserConfig = {
  solidity: {
    version: "0.8.24",
    settings: {
      optimizer: {
        enabled: true,
        runs: 200,
      },
    },
  },
  networks: {
    besuLocal: {
      url: process.env.RPC_URL || "http://127.0.0.1:8545",
      chainId: 1337,
      accounts: [process.env.PRIVATE_KEY || DEV_PRIVATE_KEY],
      gasPrice: 2000000000,
    },
    besuConsortium: {
      url: process.env.RPC_URL || "http://127.0.0.1:8545",
      chainId: 2026,
      accounts: [process.env.PRIVATE_KEY || DEV_PRIVATE_KEY],
      gasPrice: 0,
    },
  },
};

export default config;
