# Supervisor
First Project

Descriere: Supervisor este o aplicatie care monitorizeaza toate procesele active care prezinta interfata grafica. Aceasta colecteaza informatii ca: main window title, ora pornire si durata activa.
Aplicatia poate fi folosita pentru a-l informa pe utilizator despre cat timp petrece lucrand sau fiind neproductiv (partea de productivity), sau pentru a sustine un test pe calculator la scoala,
de asemenea, firmele pot sa-si supravegheze angajatii cu ajutorul aplicatiei (partea de supervise). Acest tip de aplicatie necesita rulare in background, 
deci vine la pachet cu un Worker Service care ruleaza ca Windows Service, aplicatia avand rol de interfata grafica si meniu de setari pentru serviciu. 
Programul poate impiedica rularea unor procese nedorite la alegerea utilizatorului. Supervisor poate da un raport in legatura cu timpul petrecut de utilizator pe aplicatii sau site-uri care sunt clasificate in urmatoarele 6 categorii: Coding and Programming, 
Studying/Learning, Working , Social Media, Entertainment, Gaming. 
Tehnologii: Aplicatia a fost creata in Visual Studio 2022 cu framework-ul .NET MAUI. Worker Service-ul creat in Visual Studio 2022. Comunicarea dintre aplicatie si serviciu este facuta prin Named Pipes.
 Comunicatiile si stocarea datelor este criptata cu AES-256. Clasificarea proceselor se realizeaza prin intermediul unui ML.NET Model (machine learning) antrenat de mine.
 SMTP Client pentru trimiterea sub forma de email a raportului din urma testelor (supervise). 
