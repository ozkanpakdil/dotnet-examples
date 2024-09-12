# how to build

This project supports AOT

```shell
dotnet publish --configuration Release
```

# how to run the project

Before running make sure you run the exe from elevated access. Run cmd as Administrator

```shell
GCDumper.exe RedGate.Monitor.BaseMonitor interval2:30 interval1:15 memDump:true
```

# options

- dumpBeforeGc : Whether to dump before GC (default: true)
- memDump : Whether to generate DMP files (default: false)
- interval1 : Seconds to wait before second dump (default: IO)
- interva12 : Seconds to wait before third dump (default: 20)

# example output from the Rider(IDE deom JetBrains)

```shell
C:\Users\ozkan\AppData\Local\Programs\Rider\plugins\dpa\DotFiles\JetBrains.DPA.Runner.exe --handle=30668 --backend-pid=2668 --etw-collect-flags=67108622 --detach-event-name=dpa.detach.30668 C:/Users/ozkan/projects/dotnet-examples/GCDumper/bin/Debug/net8.0/GCDumper.exe RedGate.Monitor.BaseMonitor interval2:30 interval1:15 memDump:true
Found process 'RedGate.Monitor.BaseMonitor' with ID: 22920
Taking thread dump before GC
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\thread_dump_initial.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\dump_before_gc.dmp
forcegc: proc 22920: starting
forcegc: proc 22920:  GC #757 Induced start at 261.24ms
forcegc: proc 22920:  GC #757 Induced end, paused for 18.37ms
forcegc: proc 22920:     heap size before 52.58 MB, after 50.81 MB, 3.38 % freed.
forcegc: proc 22920:     depth                                  2
forcegc: proc 22920:     finalization promoted count           24
forcegc: proc 22920:     finalization promoted size        24,048
forcegc: proc 22920:     GC handle count                    8,898
forcegc: proc 22920:     generation size 0             10,651,624
forcegc: proc 22920:     generation size 1                273,128
forcegc: proc 22920:     generation size 2             39,282,536
forcegc: proc 22920:     generation size 3                336,504
forcegc: proc 22920:     pinned object count                    7
forcegc: proc 22920:     sink block count                      21
forcegc: proc 22920:     total heap size               50,806,104
forcegc: proc 22920:     total promoted                39,895,312
forcegc: proc 22920:     total promoted size 0            313,512
forcegc: proc 22920:     total promoted size 1            110,528
forcegc: proc 22920:     total promoted size 2         39,134,864
forcegc: proc 22920:     total promoted size 3            336,408
forcegc: proc 22920: complete.
Requested GC on process 22920
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\dump_after_gc.dmp
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\thread_dump_second.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\dump_after_15s.dmp
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\thread_dump_third.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\bin\Debug\net8.0\dump_after_30s.dmp
Process completed. Dumps have been created in the current directory.
Creating zip file...
Adding file 7 of 7 (100%)
Zip file created successfully: ./dumps_20240912220904.zip
Zip file size: 524.99 MB
Total files size: 1858.30 MB
Compression ratio: 71.75%

Process finished with exit code 0.
```
and console output from powershell
```shell
C:\Users\ozkan\projects\dotnet-examples\GCDumper [main ≡ +1 ~0 -0 | +0 ~4 -0 !]> .\bin\Release\net8.0\win-x64\native\GCDumper.exe RedGate.Monitor.BaseMonitor interval2:30 interval1:15 memDump:true
Found process 'RedGate.Monitor.BaseMonitor' with ID: 22920
Taking thread dump before GC
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\thread_dump_initial.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\dump_before_gc.dmp
forcegc: proc 22920: starting
forcegc: proc 22920:  GC #827 Induced start at 116.73ms
forcegc: proc 22920:  GC #827 Induced end, paused for 15.71ms
forcegc: proc 22920:     heap size before 59.27 MB, after 52.32 MB, 11.72 % freed.
forcegc: proc 22920:     depth                                  2
forcegc: proc 22920:     finalization promoted count          175
forcegc: proc 22920:     finalization promoted size       290,257
forcegc: proc 22920:     GC handle count                    8,673
forcegc: proc 22920:     generation size 0             12,000,368
forcegc: proc 22920:     generation size 1                492,536
forcegc: proc 22920:     generation size 2             39,229,896
forcegc: proc 22920:     generation size 3                336,504
forcegc: proc 22920:     pinned object count                    7
forcegc: proc 22920:     sink block count                      20
forcegc: proc 22920:     total heap size               52,321,616
forcegc: proc 22920:     total promoted                40,063,488
forcegc: proc 22920:     total promoted size 0            529,344
forcegc: proc 22920:     total promoted size 1              1,520
forcegc: proc 22920:     total promoted size 2         39,196,216
forcegc: proc 22920:     total promoted size 3            336,408
forcegc: proc 22920: complete.
Requested GC on process 22920
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\dump_after_gc.dmp
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\thread_dump_second.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\dump_after_15s.dmp
Thread dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\thread_dump_third.txt
Dump created at C:\Users\ozkan\projects\dotnet-examples\GCDumper\dump_after_30s.dmp
Process completed. Dumps have been created in the current directory.
Creating zip file...
Adding file 7 of 7 (100%)
Zip file created successfully: ./dumps_20240912223502.zip
Zip file size: 534.15 MB
Total files size(all dumps): 1920.13 MB
Compression ratio: 72.18%
Please send the zip file to the support for further investigation.
C:\Users\ozkan\projects\dotnet-examples\GCDumper [main ≡ +1 ~0 -0 | +8 ~4 -0 !]> 
```