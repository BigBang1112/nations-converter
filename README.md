# Nations Converter 2

This is where the new TMUF is being made.

## Overview

```mermaid
graph LR
    X(Create Base Map)
    Z(Environment) --> ExA
    ExA(Conversion Set)
    ExA --> ExB(Covered Zone Block Info)
    ExA --> ExC(Terrain Modifier Zone)
    X --> A(Place Base Zone)
    A --> B(Place Block)
    B --> C(Water)
    C --> D(Pylon)
    D --> E(Decoration)
    E --> F(Place Transformation)
    F --> G(MediaTracker)
    G --> H(Music)
    H --> I(Metadata)
    I --> Y(Outputted map)

    ExA --> A
    ExA --> B
    ExA --> C
    ExA --> D
    ExA --> E
    ExA --> F

    ExB --> B
    ExB --> C
    ExC --> A
    ExC --> B
    
    subgraph Extracts from input map
        X
        ExA
        ExB
        ExC
    end

    subgraph Stages
        A
        B
        C
        D
        E
        F
        G
        H
        I
    end
```

## Build

Build the solution with Visual Studio 2022 or by using `dotnet build`.

This project started using the GBX.NET nightly builds for more comfortable development.
