import { Pipe, PipeTransform } from '@angular/core';
import { SchemeComparisonDto } from '../../core/models/nav.model';

@Pipe({ name: 'navGrowCount', standalone: true })
export class NavGrowCountPipe implements PipeTransform {
  transform(schemes: SchemeComparisonDto[], growing: boolean): number {
    return schemes.filter(s => {
      const last = s.history[s.history.length - 1];
      return growing ? last?.isGrowth : !last?.isGrowth;
    }).length;
  }
}
