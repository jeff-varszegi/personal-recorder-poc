# Personal Recorder

> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

A SharpBit project: Personal recording and emergency-response system for legal observers, witnesses, and individuals in vulnerable situations.

## Project Structure

```
personal-recorder/
├── components/
│   ├── mobile/          # React Native mobile application
│   ├── service/         # .NET web service API
│   └── contracts/
│       └── service/     # TypeScript types (generated from .NET)
├── design/              # Design documents and naming conventions
├── docs/                # Documentation and specifications
└── temp/                # Temporary files and conversation history
```

## Quick Start

### Prerequisites

- .NET 8.0+ SDK (for backend service)
- Node.js 20+ LTS (for mobile app)
- pnpm (recommended) or npm
- Android Studio (for Android development)
- Xcode (for iOS development, macOS only)

### Development Setup

```bash
# Start backend service
cd components/service
dotnet build
dotnet run

# In a separate terminal, start mobile app development
cd components/mobile
pnpm install
pnpm start           # Start Metro bundler
pnpm android         # Run on Android emulator
pnpm ios             # Run on iOS simulator (macOS only)
```

### Rapid Development

- **Hot Reload**: Edit code, see changes in ~1-2 seconds
- **Metro Bundler**: JavaScript bundler for React Native
- **Flipper**: Debug tool for network, state, and logs

## Documentation

- [Technical Specification](./TECHNICAL_SPECIFICATION.md)
- [Architecture Decision Records](./docs/adr/) (coming soon)

## License

TBD - See [Open Questions in Technical Specification](./TECHNICAL_SPECIFICATION.md#11-open-questions--decisions-needed)
