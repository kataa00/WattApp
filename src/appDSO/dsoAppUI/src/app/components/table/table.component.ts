import { animate, state, style, transition, trigger } from '@angular/animations';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.css'],
  animations: [
     trigger('detailExpand', [
      state('collapsed', style({height: '0px', minHeight: '0'})),
      state('expanded', style({height: '*'})),
      transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
    ]),
  ]
})
export class TableComponent {

  // dataSource = ;
  // columnsToDisplay = ;
  // columnsToDisplayExpaned = ;
  // expandedElement?: null;
  
  constructor(
    private http : HttpClient
  ){}

  // getAllUsers(){
  //   return this.http.get('');
  // }

}

export interface UsersTable{
  fullName: string;
  cityStreet: string;
  production: string,
  consumption: string,
  cord: string;

}