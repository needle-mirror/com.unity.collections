# Allocator performance benchmarks

## Performance test results of single thread allocations

|  **Test Name** | **Allocator** |  **Min [ms]**  |  **Max [ms]**   |  **Median [ms]** |  **Average [ms]** |  **StandardDeviation [ms]** |  **Sum [ms]**     |
|---|---|---|---|---|---|---|---|
| **Fixed small size** <br/>Iteration: 150<br/> Allocation size: 1KB | Allocator.Temp	| 0.0069 | 0.0225 | 0.0123  | 0.012416 | 0.003172 | 1.2416 |
| | Allocator.TempJob	| 0.0147 | 0.0366 | 0.0276  | 0.025610 | 0.006074 | 2.5610 |
| | Allocator.Persistent	| 0.0303 | 0.0971 | 0.0550  | 0.055878 | 0.016788 | 5.5878 |
| | Rewindable Allocator	| 0.0095 | 0.0345 | 0.0156  | 0.015427 | 0.004074 | 1.5427 |
| **Fixed large size** <br/>Iteration: 150<br/> Allocation size: 1024KB | Allocator.Temp	| 0.0069 | 0.6691 | 0.0225  | 0.164623 | 0.166818 | 32.9245 |
| | Allocator.TempJob	| 0.0147 | 2.1107 | 0.0298  | 0.058806 | 0.237314 | 11.7611 |
| | Allocator.Persistent	| 0.0303 | 0.8955 | 0.4400  | 0.300419 | 0.254459 | 60.0837 |
| | Rewindable Allocator	| 0.0095 | 0.0345 | 0.0168  | 0.016386 | 0.004199 | 3.2771 |
| **Incremental size** <br/>Iteration: 150<br/> Allocation sizes: 64KB, 128KB,   192KB, ... , 150 * 64KB | Allocator.Temp	| 0.0069 | 2.5460 | 0.2868  | 0.546976 | 0.637190 | 164.0927 |
| | Allocator.TempJob	| 0.0147 | 8.1429 | 0.0346  | 0.423294 | 0.826510 | 126.9881 |
| | Allocator.Persistent	| 0.0303 | 2.3746 | 0.5211  | 0.795122 | 0.739539 | 238.5366 |
| | Rewindable Allocator	| 0.0082 | 0.2171 | 0.0165  | 0.016722 | 0.012312 | 5.0165 |
| **Decremental size** <br/>Iteration: 150<br/> Allocation sizes: 150 * 64KB, ... , 192KB, 128KB, 64KB | Allocator.Temp	| 0.0069 | 2.5460 | 0.3660  | 0.733695 | 0.692608 | 293.4778 |
| | Allocator.TempJob	| 0.0147 | 8.1429 | 0.5043  | 0.482652 | 0.811037 | 193.0606 |
| | Allocator.Persistent	| 0.0303 | 7.1636 | 1.3603  | 1.009566 | 0.795530 | 403.8265 |
| | Rewindable Allocator	| 0.0082 | 0.2171 | 0.0161  | 0.016123 | 0.010891 | 6.4490 |

## Performance results of multithreaded allocations

|  **Test Name** | **Allocator** |  **Min [ms]**  |  **Max [ms]**   |  **Median [ms]** |  **Average [ms]** |  **StandardDeviation [ms]** |  **Sum [ms]**     |
|---|---|---|---|---|---|---|---|
| **Fixed small size** <br/>Iteration: 150<br/> Allocation size: 1KB | Allocator.Temp	| 0.0069 | 2.5460 | 0.5407  | 0.761082 | 0.658900 | 380.5408 |
| | Allocator.TempJob	| 0.0147 | 8.1429 | 0.0748  | 0.408060 | 0.743674 | 204.0298 |
| | Allocator.Persistent	| 0.0303 | 7.1636 | 0.9878  | 1.003460 | 0.712059 | 501.7299 |
| | Rewindable Allocator	| 0.0082 | 0.3902 | 0.0173  | 0.071357 | 0.111812 | 35.6783 |
| **Fixed large size** <br/>Iteration: 150<br/> Allocation size: 1024KB | Allocator.Temp	| 0.0069 | 9.7534 | 1.1779  | 1.285169 | 1.424074 | 771.1016 |
| | Allocator.TempJob	| 0.0147 | 11.2457 | 0.5053  | 1.836770 | 3.278092 | 1102.0621 |
| | Allocator.Persistent	| 0.0303 | 9.9808 | 1.3603  | 2.172288 | 2.709125 | 1303.3725 |
| | Rewindable Allocator	| 0.0082 | 0.3902 | 0.0183  | 0.106714 | 0.129213 | 64.0285 |
| **Incremental size** <br/>Iteration: 150<br/> Allocation sizes: 64KB, 128KB,   192KB, ... , 150 * 64KB | Allocator.Temp	| 0.0069 | 42.7420 | 1.2944  | 3.443426 | 5.889106 | 2410.3981 |
| | Allocator.TempJob	| 0.0147 | 27.8822 | 0.5726  | 4.930602 | 8.179837 | 3451.4214 |
| | Allocator.Persistent	| 0.0303 | 23.9661 | 1.5408  | 4.762563 | 6.840621 | 3333.7942 |
| | Rewindable Allocator	| 0.0082 | 0.4444 | 0.0199  | 0.132762 | 0.135939 | 92.9334 |
| **Decremental size** <br/>Iteration: 150<br/> Allocation sizes: 150 * 64KB, ... , 192KB, 128KB, 64KB | Allocator.Temp	| 0.0069 | 42.7420 | 1.4262  | 4.965762 | 7.040747 | 3972.6092 |
| | Allocator.TempJob	| 0.0147 | 27.8822 | 0.8252  | 7.246223 | 9.822224 | 5796.9781 |
| | Allocator.Persistent	| 0.0303 | 30.5407 | 1.6479  | 6.824334 | 8.427697 | 5459.4675 |
| | Rewindable Allocator	| 0.0082 | 0.4444 | 0.1920  | 0.149789 | 0.135044 | 119.8308 |

