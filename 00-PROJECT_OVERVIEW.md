# Screenshot Manager - Project Overview

## Project Purpose

This project demonstrates modern desktop application development using .NET 8 with a **privacy-first, local-only approach**. Through building a practical screenshot management system, we showcase secure, offline-capable desktop development with Azure AI integration and real-time user interfaces.

## Primary Objectives

### Technical Demonstration
- **System Tray Application** with global hotkey integration (Ctrl+Print Screen)
- **Local File Storage** for secure, privacy-first data management
- **WPF Desktop Interface** with modern MVVM patterns
- **Azure AI Vision Integration** with background processing queues
- **Component-Based Architecture** for maintainable code
- **Real-Time Updates** with event-driven architecture
- **.NET 8** latest features utilization

### Development Methodology Practice
- **Specification-Driven Development** - Documentation-first approach
- **Component-Based Design** - Maintainable architecture
- **Incremental Development** - Crawl/Walk/Run approach
- **AI Pair Programming** - Collaborative development with Claude Code

## Target Users

### Primary Users
- Business professionals who frequently take screenshots
- Project managers who need to organize screen captures as documentation
- UI/UX designers, developers, and product managers

### Secondary Users
- Team members involved in screen sharing and review processes
- Technical documentation authors

## Problems to Solve

### Current Issues
- Screenshots scattered across local folders
- Limitations of filename-based organization
- Difficulty searching through historical screen content
- Inefficient team sharing processes

### Value Proposition
- **Privacy Protection**: All screenshots stored locally, only analyzed when user captures
- **System Integration**: Global hotkey (Ctrl+Print Screen) for instant capture anywhere
- **Real-Time AI Analysis**: Background Azure AI Vision processing with live gallery updates
- **Rich Desktop Experience**: Modern WPF interface with search, management, and statistics
- **Fast Performance**: Direct file system access with optimized thumbnails and metadata
- **Zero Storage Costs**: Use your own disk space, pay only for AI analysis API calls

## Technical Constraints

### Required Components
- .NET 8 or higher
- Windows desktop (for system tray and global hotkey support)
- Local file system access for screenshot storage

### Optional Components
- Azure AI Vision API key for content analysis
- Internet connection for AI processing (analysis can be skipped if offline)

## Success Criteria

### As Technical Demonstration
- [x] Privacy-first local storage implementation
- [x] Modern .NET 8 component architecture
- [x] Incremental development methodology demonstration
- [x] Azure AI Vision integration with background processing
- [x] Real-time event-driven updates
- [ ] Complete system tray application integration

### As Practical Application
- [x] Global hotkey integration (Ctrl+Print Screen)
- [x] Interactive area selection for screenshots
- [x] Reliable local file management with optimization
- [x] Rich WPF gallery interface with search capabilities
- [x] Background AI analysis with real-time results
- [ ] System tray application deployment

## Project Constraints

### Time Constraints
- Each stage must be independently functional and complete
- Always maintain demo-ready state

### Technical Constraints
- Local storage limitations (user's available disk space)
- Adherence to privacy and security best practices
- Cross-platform compatibility requirements

### Feature Constraints
- Crawl stage: Basic file management only
- Walk stage: Incremental AI feature addition
- Run stage: Advanced analysis and search capabilities

## Next Steps

1. Detailed roadmap planning (01-ROADMAP.md)
2. System architecture design (02-ARCHITECTURE.md)
3. Feature specification detailing (03-FEATURES.md)
4. Technical implementation details (04-TECHNICAL_DESIGN.md)

---

**Last Updated**: June 17, 2025  
**Version**: 1.0  
**Status**: Planning Complete
