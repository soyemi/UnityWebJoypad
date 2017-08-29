<template>
    <div>
        <button @click="connect">Connect</button>
        <button @click="sendMsg">send msg</button>
        <p>{{logs}}</p>
    </div>
</template>
<script>

const W3CWebSocket = require('websocket').w3cwebsocket;

export default {
    data() {
        return {
            client: null,
            logs: "",
        }
    },
    methods: {
        connect() {
            this.client = new W3CWebSocket('ws://192.168.1.3:8885');
            this.client.onerror = function() {
                console.log('connection error');
                this.logs = "connection error";
            };
            this.client.onopen = function() {
                console.log('websocket client connected');
                this.send("here is the client");

                this.onmessage = function(event) {
                console.log('received event', event);
                };
                this.onclose = function(event) {
                    console.log("close event", event);
                };
            };
            
        },
        sendMsg() {
            this.client.send("thick");
        }
    },
    mounted() {
    },
}
</script>

<style>

</style>
