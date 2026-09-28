# Time Capture — Aderant intern briefing demo

给明天 Albany 面试用的小产品：律师工时录入。不是完整 Expert，只做 briefing 里会开错账单的那几条。

## 跑起来

```powershell
cd D:\求职\aderant-time-capture
dotnet run --project src\TimeCapture.Web
```

浏览器打开 http://localhost:5288

## 跑测试（Playwright + 接口）

```powershell
cd D:\求职\aderant-time-capture
dotnet test
```

如果缺浏览器：

```powershell
pwsh tests\TimeCapture.Tests\bin\Debug\net10.0\playwright.ps1 install
```

## 面试可以点的

1. 律师 Alice 给 Northwind / M-1001 记 0.5h → 列表出现，带 UTC。  
2. 不选 matter 点 Save → 报错，不写数据。  
3. Save 后按钮会灰（模拟延迟），同一 idempotency key 不会变成两条。  
4. PA 模式选 John Smith → 拒绝（助理不能给任意律师录）。  
5. 接口：未知律师的 Integration 写入会被拒。

## 和 briefing 的对应

- `data-testid` 钩子：timekeeper、client-search、matter-select、duration、activity-category、narrative、save-entry、todays-entries  
- 可见 label，方便 Playwright `GetByLabel` / 不爱电脑的律师  
- 时长 0.1 小时单位；comment 必填  
- 时区只影响显示，时长不变  
- Leapwork 没做桌面壳；Web 用 Playwright。现场可以说 WPF 会用同名 AutomationId。
