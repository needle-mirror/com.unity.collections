# Performance comparison

> **Note**  
>  
> This file is generated on Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz.  
> To regenerate this file locally use: DOTS -> Unity.Collections -> Generate Benchmark menu.  

## Dictionary

### Dictionary (Reference)

Using `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      1.146000 |      10000 |     0.000115 |      1.000x |
| Repeat Lookup                                  |      0.736900 |      10000 |     0.000074 |      1.000x |
| Repeat Get Count (Full)                        |      0.313600 |      10000 |     0.000031 |      1.000x |
| Repeat Remove                                  |      1.191700 |      10000 |     0.000119 |      1.000x |
| Repeat Get Count (Empty)                       |      0.097500 |      10000 |     0.000010 |      1.000x |
| Clear                                          |      0.007800 |          1 |     0.007800 |      1.000x |

### NativeHashMap (vs Dictionary) / No-Burst

Using `Unity.Collections.NativeHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      2.200400 |      10000 |     0.000220 |      0.170x |
| Repeat Lookup                                  |      0.995600 |      10000 |     0.000100 |      0.456x |
| Repeat Get Count (Full)                        |      0.413600 |      10000 |     0.000041 |      0.230x |
| Repeat Remove                                  |      1.048500 |      10000 |     0.000105 |      0.433x |
| Repeat Get Count (Empty)                       |      0.235200 |      10000 |     0.000024 |      0.425x |
| Clear                                          |      0.005100 |          1 |     0.005100 |      1.294x |

### UnsafeHashMap (vs NativeHashMap) / No-Burst

Using `Unity.Collections.UnsafeHashMap<int, int>` vs `Unity.Collections.NativeHashMap<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.954700 |      10000 |     0.000095 |      1.083x |
| Repeat Lookup                                  |      0.749300 |      10000 |     0.000075 |      0.816x |
| Repeat Get Count (Full)                        |      0.258200 |      10000 |     0.000026 |      0.883x |
| Repeat Remove                                  |      0.657900 |      10000 |     0.000066 |      1.127x |
| Repeat Get Count (Empty)                       |      0.089400 |      10000 |     0.000009 |      2.669x |
| Clear                                          |      0.004100 |          1 |     0.004100 |      0.659x |

### NativeParallelHashMap (vs Dictionary) / No-Burst

Using `Unity.Collections.NativeParallelHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      1.653700 |      10000 |     0.000165 |      0.232x |
| Repeat Lookup                                  |      1.297200 |      10000 |     0.000130 |      0.342x |
| Repeat Get Count (Full)                        |      5.945500 |      10000 |     0.000595 |      0.016x |
| Repeat Remove                                  |      1.205500 |      10000 |     0.000121 |      0.380x |
| Repeat Get Count (Empty)                       |      0.329200 |      10000 |     0.000033 |      0.285x |
| Clear                                          |      0.005500 |          1 |     0.005500 |      1.182x |

### UnsafeParallelHashMap (vs Dictionary) / No-Burst

Using `Unity.Collections.UnsafeParallelHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.798100 |      10000 |     0.000080 |      0.468x |
| Repeat Lookup                                  |      0.641300 |      10000 |     0.000064 |      0.706x |
| Repeat Get Count (Full)                        |      5.883600 |      10000 |     0.000588 |      0.017x |
| Repeat Remove                                  |      0.656700 |      10000 |     0.000066 |      0.710x |
| Repeat Get Count (Empty)                       |      0.145700 |      10000 |     0.000015 |      0.666x |
| Clear                                          |      0.004600 |          1 |     0.004600 |      1.457x |

### NativeHashMap (vs Dictionary) + Burst

Using `Unity.Collections.NativeHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.203100 |      10000 |     0.000020 |      1.860x |
| Repeat Lookup                                  |      0.067700 |      10000 |     0.000007 |      6.677x |
| Repeat Get Count (Full)                        |      0.000000 |      10000 |     0.000000 |   Infinityx |
| Repeat Remove                                  |      0.146400 |      10000 |     0.000015 |      3.230x |
| Repeat Get Count (Empty)                       |      0.000100 |      10000 |     0.000000 |    963.993x |
| Clear                                          |      0.002800 |          1 |     0.002800 |      2.357x |

### NativeParallelHashMap (vs Dictionary) + Burst

Using `Unity.Collections.NativeParallelHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.113700 |      10000 |     0.000011 |      3.314x |
| Repeat Lookup                                  |      0.013000 |      10000 |     0.000001 |     35.362x |
| Repeat Get Count (Full)                        |      0.482400 |      10000 |     0.000048 |      0.191x |
| Repeat Remove                                  |      0.090000 |      10000 |     0.000009 |      5.198x |
| Repeat Get Count (Empty)                       |      0.000100 |      10000 |     0.000000 |    963.133x |
| Clear                                          |      0.002500 |          1 |     0.002500 |      2.640x |

### UnsafeHashMap(vs NativeHashMap) + Burst

Using `Unity.Collections.UnsafeHashMap<int, int>` (compiled with Burst) vs `Unity.Collections.NativeHashMap<int, int>` (compiled with Burst).

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.138000 |      10000 |     0.000014 |      1.470x |
| Repeat Lookup                                  |      0.065400 |      10000 |     0.000007 |      1.034x |
| Repeat Get Count (Full)                        |      0.000000 |      10000 |     0.000000 |   Infinityx |
| Repeat Remove                                  |      0.072400 |      10000 |     0.000007 |      2.046x |
| Repeat Get Count (Empty)                       |      0.000000 |      10000 |     0.000000 |        NaNx |
| Clear                                          |      0.002300 |          1 |     0.002300 |      1.087x |

### UnsafeParallelHashMap (vs Dictionary) + Burst

Using `Unity.Collections.UnsafeParallelHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>` (compiled with Burst).

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Insert                                  |      0.141000 |      10000 |     0.000014 |      2.655x |
| Repeat Lookup                                  |      0.067300 |      10000 |     0.000007 |      6.802x |
| Repeat Get Count (Full)                        |      0.000000 |      10000 |     0.000000 |   Infinityx |
| Repeat Remove                                  |      0.072400 |      10000 |     0.000007 |      6.449x |
| Repeat Get Count (Empty)                       |      0.000100 |      10000 |     0.000000 |    984.993x |
| Clear                                          |      0.002400 |          1 |     0.002400 |      2.875x |

## List

### List (Reference)

Using `System.Collections.Generic.List<int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Add                                     |      0.221400 |      10000 |     0.000022 |      1.000x |
| Repeat Lookup                                  |      0.241100 |      10000 |     0.000024 |      1.000x |
| Repeat Insert                                  |     11.929000 |      10000 |     0.001193 |      1.000x |
| Repeat Remove                                  |      8.232100 |      10000 |     0.000823 |      1.000x |
| Repeat RemoveAtSwapBack                        |      2.418100 |      10000 |     0.000242 |      1.000x |
| Clear                                          |      0.002400 |          1 |     0.002400 |      1.000x |

### NativeList (vs List) / No-Burst

Using `Unity.Collections.NativeList<int>` vs `System.Collections.Generic.List<int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Add                                     |      1.261200 |      10000 |     0.000126 |      0.088x |
| Repeat Lookup                                  |      0.864300 |      10000 |     0.000086 |      0.126x |
| Repeat Insert                                  |     14.618200 |      10000 |     0.001462 |      0.808x |
| Repeat Remove                                  |      9.794100 |      10000 |     0.000979 |      0.775x |
| Repeat RemoveAtSwapBack                        |      2.378400 |      10000 |     0.000238 |      1.022x |
| Clear                                          |      0.002000 |          1 |     0.002000 |      0.300x |

### UnsafeList (vs NativeList) / No-Burst

Using `Unity.Collections.UnsafeList<int>` vs `Unity.Collections.NativeList<int>`.

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Add                                     |      0.880500 |      10000 |     0.000088 |      1.139x |
| Repeat Lookup                                  |      0.508900 |      10000 |     0.000051 |      1.104x |
| Repeat Insert                                  |     13.069700 |      10000 |     0.001307 |      1.018x |
| Repeat Remove                                  |      8.424800 |      10000 |     0.000842 |      1.055x |
| Repeat RemoveAtSwapBack                        |      1.680600 |      10000 |     0.000168 |      1.048x |
| Clear                                          |      0.002000 |          1 |     0.002000 |      0.250x |

### NativeList (vs List) + Burst

Using `Unity.Collections.NativeList<int>` vs `System.Collections.Generic.List<int>` (compiled with Burst).

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Add                                     |      0.077200 |      10000 |     0.000008 |      1.367x |
| Repeat Lookup                                  |      0.000000 |      10000 |     0.000000 |   Infinityx |
| Repeat Insert                                  |      8.732400 |      10000 |     0.000873 |      1.349x |
| Repeat Remove                                  |      8.562700 |      10000 |     0.000856 |      0.898x |
| Repeat RemoveAtSwapBack                        |      0.121700 |      10000 |     0.000012 |     18.103x |
| Clear                                          |      0.000100 |          1 |     0.000100 |      6.000x |

### UnsafeList (vs NativeList) + Burst

Using `Unity.Collections.UnsafeList<int>` vs `Unity.Collections.NativeList<int>` (compiled with Burst).

| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |
| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |
| Repeat Add                                     |      0.018300 |      10000 |     0.000002 |      4.224x |
| Repeat Lookup                                  |      0.000100 |      10000 |     0.000000 |      1.000x |
| Repeat Insert                                  |     10.339000 |      10000 |     0.001034 |      0.897x |
| Repeat Remove                                  |      8.424000 |      10000 |     0.000842 |      0.990x |
| Repeat RemoveAtSwapBack                        |      0.036400 |      10000 |     0.000004 |      3.346x |
| Clear                                          |      0.000100 |          1 |     0.000100 |      0.000x |

