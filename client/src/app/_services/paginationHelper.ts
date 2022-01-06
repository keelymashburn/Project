import { HttpClient, HttpParams } from "@angular/common/http";
import { PaginatedResult } from "../_models/pagination";
import { map } from 'rxjs/operators';


export function getPaginatedResult<T>(url, params, http: HttpClient)
{
  const paginatedResult: PaginatedResult<T> = new PaginatedResult<T>();
  return http.get<T>(url, { observe : 'response', params }).pipe(
    map(res =>
      {
        paginatedResult.result = res.body;
        if(res.headers.get('Pagination') !== null)
        {
          paginatedResult.pagination = JSON.parse(res.headers.get('Pagination'));
        }
        return paginatedResult; 
      })
  );
}

export function getPaginationHeaders(pageNumber: Number, pageSize: Number)
{
  let params = new HttpParams(); 

  params = params.append('pageNumber', pageNumber.toString());
  params = params.append('pageSize', pageSize.toString());

  return params;
}