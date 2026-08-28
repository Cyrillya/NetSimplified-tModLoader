# 发包

发包调用 `NetModule.Send` 即可，获取包实例看上面的

## 传参

- `toClient` -> 如果不是 `-1`, 则包<b>只会</b>发送给对应的客户端
- `ignoreClient` -> 如果不是 `-1`, 则包<b>不会</b>发送给对应的客户端
- `runLocally` -> 如果为 `true` 则在发包时会调用相应 `NetModule` 包的 `NetModule.Receive()` 方法，<b>默认为 `false`</b>

若 `toClient` 和 `ignoreClient` 皆为 `-1` 时

- 在服务器调用 `Send` -> 发给所有客户端
- 在客户端调用 `Send` -> 发给服务器
  

[返回 README](../README.md)
