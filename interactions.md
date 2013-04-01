interactions
======

### Meta-Server
###### OPEN:
- O metaserver est‡ on?
- Ficheiro existe?
- Actualiza a lista de ficheiros abertos;
- Actualiza a heat-table;
- Retorna o file-handler ao cliente.

CLOSE:
- O metaserver est‡ on?
- Ficheiro est‡ bloqueado?
- Actualiza a tabela de ficheiros abertos.

CREATE:
- O metaserver est‡ on?
- Ficheiro existe?
- Cria file-handler e faz o lock()
- Retorna o file-handler para o cliente.

CONFIRM-CREATE:
- O metaserver est‡ on?
- Faz unlock() ao ficheiro;
- Cria entrada na lista de ficheiros abertos.

DELETE:
- O metaserver est‡ on?
- Ficheiro existe?
- Ficheiro est‡ bloqueado?
- Bloqueia o ficheiro
- Retorna o file-handler para o cliente.

CONFIRM-DELETE:
- O metaserver est‡ on?
- Retira o ficheiro de tabela de file-handlers
- Desbloqueia o acesso do nome.

WRITE:
- O metaserver est‡ on?
- Ficheiro existe?
- Ficheiro est‡ bloqueado?
- O cliente tem o ficheiro aberto?
- Faz lock() ao ficheiro;
- Retorna o file-handler.

CONFIRM-WRITE:
- O metaserver est‡ on?
- Actualiza file-handler;
- Faz unlock() ao ficheiro.

FAIL:
- Mete a fail-flag a true.

RECOVER:
- Mete a fail-flag a false.

LOCK:
- O metaserver est‡ on?
- O file-handler tem um campo com a flag-lock e client-id, altera esse campo;
- Se o pedido vem do cliente manda os outros metadata servers fazer lock().

UNLOCK:
- O metaserver est‡ on?
- O pedido vem do cliente que tinha feito o lock()?
- Faz unlock aos outros metadata servers.



### Data-Server

### Client



Items marked as "✘" are not planned for implementation. 
Items marked as "✔" are complete and tested

<table>
  <thead>
    <tr><th>Object / Feature</th><th>Status</th><th>Notes</th></tr>
  </thead>
  <tbody>
    <tr> <td>- Example</td> <td>✔</td> <td>Notes about this task</td> </tr>
    <tr> <td>Client</td> <td> </td><td> </td></tr>
    <tr> <td>- Task to do</td><td> </td><td> </td></tr>
    <tr> <td>Meta-Data Server</td><td> </td><td> </td></tr>
    <tr> <td>- line caps/joins - line width, cap, join, miter limit</td><td>✔</td><td> </td></tr>
    <tr> <td>Data-Server</td><td> </td><td> </td></tr>
    <tr> <td>Puppet Master</td><td> </td><td> </td></tr>
    
  </tbody>
</table>
