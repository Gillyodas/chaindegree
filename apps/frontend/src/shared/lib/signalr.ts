import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';

export function createSignalRConnection(hubUrl: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
      },
    })
    .configureLogging(LogLevel.Warning)
    .build();
}
