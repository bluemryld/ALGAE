# 📂 ALGAE Signatures Repository Structure

This document outlines the proposed GitHub repository structure for hosting ALGAE game signatures.

## 🏗️ Repository Setup

### Repository Details
- **Name:** `algae-game-signatures`
- **Description:** Community-maintained game signature database for ALGAE (Advanced Launcher for Games and Associated Executables)
- **Public:** Yes (to allow community contributions)
- **URL:** `https://github.com/your-username/algae-game-signatures`

### Repository Structure
```
algae-game-signatures/
├── README.md                           # Repository overview and usage
├── CONTRIBUTING.md                     # Contribution guidelines
├── LICENSE                            # MIT License
├── .github/
│   ├── workflows/
│   │   ├── validate-signatures.yml   # GitHub Actions for validation
│   │   └── auto-merge.yml            # Auto-merge approved PRs
│   ├── ISSUE_TEMPLATE/
│   │   ├── signature-request.md      # Template for requesting new signatures
│   │   └── signature-update.md       # Template for updating existing signatures
│   └── PULL_REQUEST_TEMPLATE.md      # Template for signature submissions
├── signatures/
│   ├── game_signatures.json         # Main signatures database
│   ├── metadata.json                # Version info and statistics
│   ├── categories/                   # Organized by categories
│   │   ├── aaa-games.json           # AAA titles
│   │   ├── indie-games.json         # Indie games
│   │   ├── steam-games.json         # Steam-specific
│   │   ├── epic-games.json          # Epic Games Store
│   │   ├── microsoft-games.json     # Xbox/Game Pass
│   │   └── retro-games.json         # Classic/retro games
│   └── schema/
│       └── signature-schema.json    # JSON schema for validation
├── tools/
│   ├── validator.py                 # Python script to validate signatures
│   ├── merger.py                    # Script to merge category files
│   └── converter.py                 # Convert other formats to ALGAE format
└── docs/
    ├── API.md                       # API endpoints and usage
    ├── SIGNATURE_FORMAT.md          # Detailed signature format documentation
    └── TESTING.md                   # Testing new signatures
```

## 📝 File Formats

### Main Signatures File (`game_signatures.json`)
```json
{
  "version": "1.0.0",
  "updated": "2024-01-15T10:30:00Z",
  "signatures": [
    {
      "shortName": "TW3",
      "name": "The Witcher 3: Wild Hunt",
      "description": "Open-world RPG by CD Projekt RED",
      "executableName": "witcher3.exe",
      "publisher": "CD PROJEKT RED",
      "metaName": "The Witcher® 3: Wild Hunt",
      "matchName": true,
      "matchVersion": false,
      "matchPublisher": true,
      "gameArgs": null,
      "gameImage": null,
      "themeName": null,
      "category": "aaa-games",
      "platforms": ["steam", "gog", "epic"],
      "submittedBy": "community",
      "lastUpdated": "2024-01-15"
    }
  ]
}
```

### Metadata File (`metadata.json`)
```json
{
  "version": "1.2.3",
  "lastUpdated": "2024-01-15T10:30:00Z",
  "totalSignatures": 127,
  "categories": {
    "aaa-games": 45,
    "indie-games": 32,
    "steam-games": 89,
    "epic-games": 23,
    "microsoft-games": 15,
    "retro-games": 18
  },
  "contributors": 47,
  "apiEndpoints": {
    "latest": "https://raw.githubusercontent.com/your-username/algae-game-signatures/main/signatures/game_signatures.json",
    "metadata": "https://raw.githubusercontent.com/your-username/algae-game-signatures/main/signatures/metadata.json"
  }
}
```

## 🔄 API Endpoints

### Direct File Access (GitHub Raw)
- **Latest Signatures:** `https://raw.githubusercontent.com/your-username/algae-game-signatures/main/signatures/game_signatures.json`
- **Metadata:** `https://raw.githubusercontent.com/your-username/algae-game-signatures/main/signatures/metadata.json`
- **Schema:** `https://raw.githubusercontent.com/your-username/algae-game-signatures/main/signatures/schema/signature-schema.json`

### GitHub API Access
- **Repository Info:** `https://api.github.com/repos/your-username/algae-game-signatures`
- **File Contents:** `https://api.github.com/repos/your-username/algae-game-signatures/contents/signatures/game_signatures.json`
- **Releases:** `https://api.github.com/repos/your-username/algae-game-signatures/releases/latest`

## 🤝 Community Contribution Workflow

### 1. Adding New Signatures
1. **Fork the repository**
2. **Add signature to appropriate category file** (or main file)
3. **Validate using provided tools**
4. **Submit pull request** with signature details
5. **Community review and approval**
6. **Automated testing and merge**

### 2. Issue Templates

#### New Signature Request Template
```markdown
## Game Information
- **Game Name:** 
- **Short Name:** 
- **Executable Name:** 
- **Publisher:** 
- **Platform(s):** Steam / Epic / Microsoft Store / GOG / Other

## File Details
- **File Path:** 
- **File Size:** 
- **Version:** 
- **Product Name (from properties):** 

## Additional Information
- **Launch Arguments:** 
- **Special Notes:** 

## Verification
- [ ] I have tested this signature works correctly
- [ ] I have checked this signature doesn't already exist
- [ ] I have provided accurate information
```

### 3. Automated Validation
- **JSON Schema Validation:** Ensure signatures follow correct format
- **Duplicate Detection:** Check for existing signatures
- **Required Fields:** Validate all required fields are present
- **Naming Conventions:** Enforce consistent naming standards

## 🔧 GitHub Actions Workflows

### Signature Validation (`validate-signatures.yml`)
```yaml
name: Validate Signatures
on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-python@v4
        with:
          python-version: '3.9'
      - name: Install dependencies
        run: pip install jsonschema
      - name: Validate signatures
        run: python tools/validator.py
      - name: Check for duplicates
        run: python tools/duplicate-checker.py
```

### Auto-Merge Approved PRs (`auto-merge.yml`)
```yaml
name: Auto-merge approved PRs
on:
  pull_request_review:
    types: [submitted]

jobs:
  auto-merge:
    if: github.event.review.state == 'approved'
    runs-on: ubuntu-latest
    steps:
      - name: Auto-merge
        uses: pascalgn/merge-action@v0.15.6
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          merge_method: squash
```

## 📊 Versioning Strategy

### Semantic Versioning
- **Major (X.0.0):** Breaking changes to signature format
- **Minor (0.X.0):** New signatures or non-breaking format changes
- **Patch (0.0.X):** Bug fixes, corrections, or minor updates

### Release Process
1. **Weekly Releases:** Automated releases with new signatures
2. **Changelog Generation:** Automatic changelog from commits
3. **Tagged Releases:** Each release tagged for easy access
4. **Release Notes:** Include new games, updates, and fixes

## 🛡️ Quality Assurance

### Validation Rules
- **Required Fields:** name, executableName must be present
- **Unique Signatures:** No duplicates based on name + executable
- **Valid JSON:** Proper JSON formatting
- **Consistent Naming:** Follow naming conventions
- **File Path Validation:** Executable names should be realistic

### Testing Requirements
- **Signature Accuracy:** Must correctly identify the specified game
- **No False Positives:** Should not match unrelated executables
- **Performance Impact:** Should not significantly slow down scanning

## 🔒 Security Considerations

### Repository Security
- **No Executable Files:** Only JSON and documentation files
- **Restricted Write Access:** Only maintainers can merge directly
- **Review Required:** All changes require review before merge
- **Audit Trail:** Full history of all changes tracked

### Content Validation
- **Malicious Content:** No executable code in JSON files
- **Path Injection:** Validate file paths don't contain harmful sequences
- **Size Limits:** Prevent excessively large submissions

## 📈 Analytics and Monitoring

### Usage Statistics
- **Download Counts:** Track signature database downloads
- **Popular Games:** Most requested signatures
- **Platform Distribution:** Usage across different game platforms
- **Update Frequency:** How often signatures are updated

### Community Metrics
- **Contributors:** Number of active contributors
- **Pull Requests:** Submission and merge rates
- **Issues:** Response times and resolution rates
- **Feedback:** Community satisfaction and suggestions

## 🚀 Implementation Steps

1. ✅ **Create Repository:** Set up GitHub repository with structure
2. ✅ **Initialize Files:** Create initial signature database and metadata
3. ⬜ **Setup Actions:** Configure GitHub Actions for validation
4. ⬜ **Create Templates:** Add issue and PR templates
5. ⬜ **Write Documentation:** Complete README and contribution guides
6. ⬜ **Test Integration:** Verify ALGAE can download and use signatures
7. ⬜ **Community Launch:** Announce repository to community
8. ⬜ **Gather Feedback:** Iterate based on community feedback

This structure provides a scalable, community-driven approach to maintaining game signatures while ensuring quality and security.
