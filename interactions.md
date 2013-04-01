interactions
======

## Meta-Server
###### OPEN:
- O metaserver est‡ on?
- Ficheiro existe?
- Actualiza a lista de ficheiros abertos;
- Actualiza a heat-table;
- Retorna o file-handler ao cliente.

###### CLOSE:
- O metaserver est‡ on?
- Ficheiro est‡ bloqueado?
- Actualiza a tabela de ficheiros abertos.

###### CREATE:
- O metaserver est‡ on?
- Ficheiro existe?
- Cria file-handler e faz o lock()
- Retorna o file-handler para o cliente.

###### CONFIRM-CREATE:
- O metaserver est‡ on?
- Faz unlock() ao ficheiro;
- Cria entrada na lista de ficheiros abertos.

###### DELETE:
- O metaserver est‡ on?
- Ficheiro existe?
- Ficheiro est‡ bloqueado?
- Bloqueia o ficheiro
- Retorna o file-handler para o cliente.

###### CONFIRM-DELETE:
- O metaserver est‡ on?
- Retira o ficheiro de tabela de file-handlers
- Desbloqueia o acesso do nome.

###### WRITE:
- O metaserver est‡ on?
- Ficheiro existe?
- Ficheiro est‡ bloqueado?
- O cliente tem o ficheiro aberto?
- Faz lock() ao ficheiro;
- Retorna o file-handler.

###### CONFIRM-WRITE:
- O metaserver est‡ on?
- Actualiza file-handler;
- Faz unlock() ao ficheiro.

###### FAIL:
- Mete a fail-flag a true.

###### RECOVER:
- Mete a fail-flag a false.

###### LOCK:
- O metaserver est‡ on?
- O file-handler tem um campo com a flag-lock e client-id, altera esse campo;
- Se o pedido vem do cliente manda os outros metadata servers fazer lock().

###### UNLOCK:
- O metaserver est‡ on?
- O pedido vem do cliente que tinha feito o lock()?
- Faz unlock aos outros metadata servers.


## Data-Server

###### PREPARE_WRITE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-verifica se o ficheiro existe
-verifica se ja ha outro processo a fazer prepare() sobre o mesmo ficheiro
-Guarda o byte-array numa estrutura temporaria
-Retorna true ao cliente

###### COMMIT_WRITE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-Efetua a escrita
-Retorna True ao cliente

###### PREPARE_CREATE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-verifica se o ficheiro nao existe
-verifica se ja ha outro processo a fazer prepare_create() com o mesmo nome
-Guarda o byte-array numa estrutura temporaria
-Retorna true ao cliente

###### COMMIT_WRITE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-Cria o novo ficheiro
-Retorna True ao cliente

###### PREPARE_DELETE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-verifica se o ficheiro nao existe
-verifica se ja ha outro processo a fazer prepare() sobre o mesmo ficheiro
-Retorna true ao cliente

###### COMMIT_DELETE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-Apaga o ficheirO
-Retorna True ao cliente

###### READ
-TO DO

###### TRANSFER_FILE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-verifica se o ficheiro esta disponivel localmente
-verifica se nao existe pedidos de prepare_write() prepare_delete() sobre esse ficheiro
-chama o RECEIVE_FILE() na maquina remota cujo endereco é passado por argumento
-retorna o booleano correspondente ao retorno do RECEIVE_FILE()

###### RECEIVE_FILE
-verifica se o data server ta fail()
-verifica se o data server ta freeze() (SIM: mete o pedido na queue)
-verifica se tem algum ficheiro com o mesmo nome
-recebe o ficheiro e guarda-o
-retorna true ao data-server origem

###### FREEZE
-Altera freeze_flag para true

###### UNFREEZE
-Altera freeze_flag para false
-Efetua todos os pedidos da queue por ordem de chegada
(A atualizacao do estado do data server é da responsabilidade dos servidores metadados.)

###### FAIL
-Altera fail_flag para true

###### RECOVER
-Altera fail_flag para false



## Client



