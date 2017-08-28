<template>
    <button @click="connect">Connect</button>
</template>
<script>

const W3CWebSocket  = require('websocket').w3cwebsocket;

export default {
    data (){
        return{
            client: null,
        }
    },
    methods:{
        connect(){
            this.client = new W3CWebSocket('ws://127.0.0.1:8885');
            this.client.onerror = function(){
                console.log('connection error');
            };
            this.client.onopen = function(){
                console.log('websocket client connected');
                this.send("here is the client");

                this.onmessage = function(event){
                    console.log('received event',event);
                };
                this.onclose = function(event){
                    console.log("close event",event);
                };
            };
        },
    },
    mounted(){

    },
}
</script>

<style>

</style>
