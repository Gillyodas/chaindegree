import { HardhatUserConfig } from "hardhat/config";
import "@nomicfoundation/hardhat-toolbox";
import * as dotenv from "dotenv";

dotenv.config();

// Standard Besu dev private key (pre-funded in Besu dev network)
const DEV_PRIVATE_KEY = "0xc87509a1c067bbde30f8340456276041d572619731780b91d7e6c33d1b17df0c";

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
    },
  },
};

export default config;
