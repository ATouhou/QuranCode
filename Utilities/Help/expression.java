// <XMP>
// Compute Expressions
//
// Written by Dario Alejandro Alpern (Buenos Aires - Argentina)
// Last updated July 5th, 2003, See http://www.alpertron.com.ar/ECM.HTM
//
// No part of this code can be used for commercial purposes without
// the written consent from the author. Otherwise it can be used freely
// except that you have to write somewhere in the code this header.
// 
import java.math.*;
public final class expression {
  private static final BigInteger BigInt0 = BigInteger.valueOf(0L);
  private static final BigInteger BigInt1 = BigInteger.valueOf(1L);
  private static final BigInteger BigInt2 = BigInteger.valueOf(2L);
  private static final BigInteger BigInt3 = BigInteger.valueOf(3L);

  // Errors for next routine:
  // >0: Ok.
  // -2: Number too high (more than 1000 digits).
  // -3: Intermediate expression too high (more than 2000 digits).
  // -4: Non-integer division.
  // -5: Parenthesis mismatch.
  // -6: Syntax error
  // -7: Too many parentheses.
  // -8: Invalid parameter.
  // -100: Break.
  // Operators accepted: +, -, *, /, ^, !, F(, L(, P(.

  public static int ComputeExpression(String expr, int type, BigInteger ExpressionResult[]) {
      BigInteger BigInt1 = BigInteger.valueOf(1L);
      int stackIndex = 0;
      int exprIndex = 0;
      int exprLength = expr.length();
      int i,j;
      char charValue;
      boolean leftNumberFlag = false;
      int exprIndexAux;
      int SubExprResult,len;
      BigInteger factorial;
      BigInteger stackValues[] = new BigInteger[400];
      int stackOperators[] = new int[400];

      while (exprIndex < exprLength) {
        charValue = expr.charAt(exprIndex);
        if (charValue == '!') {           // Calculating factorial.
          if (leftNumberFlag == false) {return -6;}
          len = stackValues[stackIndex].bitLength()-1;
          if (len > 16) {return -3;}
          len = stackValues[stackIndex].intValue();
          if (len < 0 || len > 5984) {return -3;}
          factorial = BigInt1;
          for (i=2; i<=len; i++) {
            factorial = factorial.multiply(BigInteger.valueOf(i));
          }
          stackValues[stackIndex] = factorial;
        }
        if (charValue == '#') {           // Calculating primorial.
          if (leftNumberFlag == false) {return -6;}
          len = stackValues[stackIndex].bitLength()-1;
          if (len > 16) {return -3;}
          len = stackValues[stackIndex].intValue();
          if (len < 0 || len > 46049) {return -3;}
          factorial = BigInt1;
          // Check if number is prime
          for (i=2; i*i<=len; i++) {
            if (len/i*i==len) {return -8;}
          }
          factorial = BigInt1;
          compute_primorial_loop:
            for (i=2; i<=len; i++) {
            for (j=2; j*j<=i; j++) {
              if (i/j*j==i) {continue compute_primorial_loop;}
            }
            factorial = factorial.multiply(BigInteger.valueOf(i));
          }
          stackValues[stackIndex] = factorial;
        }
        if (charValue == 'B' || charValue == 'b' ||
            charValue == 'N' || charValue == 'n' ||
            charValue == 'F' || charValue == 'f' ||
            charValue == 'P' || charValue == 'p' ||
            charValue == 'L' || charValue == 'l') {
          if (leftNumberFlag || exprIndex == exprLength-1) {
            return -6;
          }
          exprIndex++;
          if (expr.charAt(exprIndex) != '(') {return -6;}
          if (stackIndex > 395) {return -7;}
          stackOperators[stackIndex++] = charValue & 0xDF; /* Convert to uppercase */
          charValue = '(';
        }
        if (charValue == '+' || charValue == '-') {
          if (leftNumberFlag == false) {      // Unary plus/minus operator
            exprIndex++;
            if (charValue == '+') {
              continue;
              }
            else {
              if (stackIndex > 0 && stackOperators[stackIndex-1] == '_') {
                stackIndex--;
                continue;
                }
              if (stackIndex > 395) {return -7;}
              stackOperators[stackIndex++] = '_'; /* Unitary minus */
              continue;
              }
            }
          if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
            if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
              return SubExprResult;
            }
            if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
              if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                return SubExprResult;
              }
              if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
                if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                  return SubExprResult;
                }
              }                         /* end if */
            }                           /* end if */
          }                             /* end if */
          stackOperators[stackIndex++] = charValue;
          leftNumberFlag = false;
        }                               /* end if */
        else {
          if (charValue == '*' || charValue == '/' || charValue == '%') {
            if (leftNumberFlag == false) {return -6;}
            if (stackIndex > 0 && (stackOperators[stackIndex-1] == '^' ||
                  stackOperators[stackIndex-1] == '*' ||
                  stackOperators[stackIndex-1] == '/' ||
                  stackOperators[stackIndex-1] == '%' ||
                  stackOperators[stackIndex-1] == 'B' ||
                  stackOperators[stackIndex-1] == 'N' ||
                  stackOperators[stackIndex-1] == 'F' ||
                  stackOperators[stackIndex-1] == 'L' ||
                  stackOperators[stackIndex-1] == 'P')) {
              if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                return SubExprResult;
              }
              if (stackIndex > 0 && (stackOperators[stackIndex-1] == '^' ||
                    stackOperators[stackIndex-1] == '*' ||
                    stackOperators[stackIndex-1] == '/' ||
                    stackOperators[stackIndex-1] == '%' ||
                    stackOperators[stackIndex-1] == 'B' ||
                    stackOperators[stackIndex-1] == 'N' ||
                    stackOperators[stackIndex-1] == 'F' ||
                    stackOperators[stackIndex-1] == 'L' ||
                    stackOperators[stackIndex-1] == 'P')) {
                if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                  return SubExprResult;
                }
              }                         /* end if */
            }                           /* end if */
            stackOperators[stackIndex++] = charValue;
            leftNumberFlag = false;
          }                             
          else {
            if (charValue == '^') {
              if (leftNumberFlag == false) {return -6;}
              if (stackIndex > 0 && (stackOperators[stackIndex-1] == '^' ||
                    stackOperators[stackIndex-1] == 'B' ||
                    stackOperators[stackIndex-1] == 'N' ||
                    stackOperators[stackIndex-1] == 'F' ||
                    stackOperators[stackIndex-1] == 'L' ||
                    stackOperators[stackIndex-1] == 'P')) {
                if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                  return SubExprResult;
                }
              }                         /* end if */
              stackOperators[stackIndex++] = charValue;
              leftNumberFlag = false;
            }                           /* end if */
            else {
              if (charValue == '(') {
                if (leftNumberFlag == true) {return -6;}
                if (stackIndex > 395) {return -7;}
                stackOperators[stackIndex++] = charValue;
              }                           
              else {
                if (charValue == ')') {
                  if (leftNumberFlag == false) {return -6;}
                  if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
                    if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                      return SubExprResult;
                    }
                    if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
                      if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                        return SubExprResult;
                      }
                      if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
                        if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
                          return SubExprResult;
                        }
                      }
                    }
                  }
                  if (stackIndex == 0) {return -5;}
                  stackIndex--;             /* Discard ")" */
                  stackValues[stackIndex] = stackValues[stackIndex+1];
                  leftNumberFlag = true;
                }
                else {
                  if (charValue >= '0' && charValue <= '9') {
                    exprIndexAux = exprIndex;
                    while (exprIndexAux < exprLength-1) {
                      charValue = expr.charAt(exprIndexAux+1);
                      if (charValue >= '0' && charValue <= '9') {
                        exprIndexAux++;
                      }
                      else {
                        break;
                      }
                    }
                    stackValues[stackIndex] = new BigInteger(expr.substring(exprIndex,exprIndexAux+1));
                    leftNumberFlag = true;
                    exprIndex = exprIndexAux;
                  }                  /* end if number */
                }                    /* end if ) */
              }                      /* end if ( */
            }                        /* end if ^ */
          }                          /* end if *, / */
        }                            /* end if +, - */
        exprIndex++;
      }                              /* end while */
      if (leftNumberFlag == false) {return -6;}
      if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
        if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
          return SubExprResult;
        }
        if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
          if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
            return SubExprResult;
          }
          if (stackIndex > 0 && stackOperators[stackIndex-1] != '(') {
            if ((SubExprResult = ComputeSubExpr(--stackIndex, stackValues, stackOperators)) != 0) {
              return SubExprResult;
            }
          }
        }
      }
      if (stackIndex != 0) {return -5;}
      if (stackValues[0].compareTo(BigInt1) <= 0 && type==0) {return -1;}
      if (stackValues[0].bitLength() > 33219) {return -2;}
      ExpressionResult[0] = stackValues[0];
      return 0;
  }

  private static int ComputeSubExpr(int stackIndex, BigInteger [] stackValues, int [] stackOperators) {
      int i, j, k, u, len, val, Tmp, indexL, indexH, count, indexNew;
      double logarithm, Tmp1, Tmp2, Tmp3;
      long DosALa63 = 1L << 63;
      BigInteger FibonPrev, FibonAct, FibonNext;
      long Part[];
      int Index[];
      byte Conv[];
      int stackOper;
      long Cy, Result;

      stackOper = stackOperators[stackIndex];
      switch (stackOper) {
        case '+':
          stackValues[stackIndex] = stackValues[stackIndex].add(stackValues[stackIndex+1]);
          return 0;
        case '-':
          stackValues[stackIndex] = stackValues[stackIndex].subtract(stackValues[stackIndex+1]);
          return 0;
        case '_':
          stackValues[stackIndex] = stackValues[stackIndex+1].negate();
          return 0;
        case '/':
          if (stackValues[stackIndex + 1].signum() == 0) {return -3;}
          if (stackValues[stackIndex].remainder(stackValues[stackIndex + 1]).
            signum() != 0) {return -4;}
          stackValues[stackIndex] = stackValues[stackIndex].divide(stackValues[stackIndex+1]);
          return 0;
        case '%':
          if (stackValues[stackIndex + 1].signum() != 0) {
            stackValues[stackIndex] = stackValues[stackIndex].remainder(stackValues[stackIndex+1]);
            }
          return 0;
        case '*':
          if (stackValues[stackIndex].bitLength() + stackValues[stackIndex+1].bitLength() > 66438) {return -3;}
          stackValues[stackIndex] = stackValues[stackIndex].multiply(stackValues[stackIndex+1]);
          return 0;
        case '^':
          len = stackValues[stackIndex].bitLength()-1;
          if (len > 32) {
            logarithm = (double)(len-32) +
                Math.log(stackValues[stackIndex].shiftRight(len-32).
                doubleValue())/Math.log(2);
            }
          else {
            logarithm = Math.log(stackValues[stackIndex].
                doubleValue())/Math.log(2);
            }
          if (logarithm * stackValues[stackIndex+1].doubleValue() > 66438) {return -3;}
          stackValues[stackIndex] = stackValues[stackIndex].pow(stackValues[stackIndex+1].intValue());
          return 0;
        case 'F':
        case 'L':
          len = stackValues[stackIndex+1].bitLength()-1;
          if (len > 17) {return -3;}
          len = stackValues[stackIndex+1].intValue();
          if (len > 95662) {return -3;}
          if (len < 0) {return -8;}
          FibonPrev = BigInteger.valueOf(stackOper == 'L'?-1:1);
          FibonAct = BigInteger.valueOf(stackOper == 'L'?2:0);
          for (i=1; i<=len; i++) {
            FibonNext = FibonPrev.add(FibonAct);
            FibonPrev = FibonAct;
            FibonAct = FibonNext;
            }
          stackValues[stackIndex] = FibonAct;
          return 0;
        case 'P':
          len = stackValues[stackIndex+1].bitLength()-1;
          if (len > 24) {return -3;}
          len = stackValues[stackIndex+1].intValue();
          if (len > 3520000) {return -3;}
          if (len < 0) {return -8;}
          len = 2;
          Tmp1 = 0.0578227587396094872;     // pi * sqrt(2/3) / log(2^64)
          Tmp2 = 0.9563674804631159673;     // 1 - log(4*sqrt(3)) / log(2^64)
          Tmp3 = 0.0225421100138900531;     // 1 / log(2^64)
          val = stackValues[stackIndex+1].intValue();
          for (i=1; i<=val; i++) {
            len += (long)(Math.floor(Tmp1 * Math.sqrt((double)i) + Tmp2 -
                   Math.log((double)i) * Tmp3));
            }
          Part = new long[len];
          Index = new int[val+2];
          Part[0] = DosALa63+1;
          Index[0] = 0;
          Index[1] = Tmp = len = 1;
          for (i=1; i<=val; i++) {
            len = Index[i] - Index[i-1] + 1;     /* Initialize number length */
            Tmp = Index[i] + len;
            for (k=Index[i]; k<Tmp; k++) {       /* Initialize number to zero */
              Part[k] = DosALa63;
              }
            for (k=1; (3*k-1)*k<=2*i; k++) {
              indexL = Index[i-(3*k-1)*k/2];
              indexH = Index[i-(3*k-1)*k/2+1];
              for (u=((3*k+1)*k<=2*i?0:1); u<=1; u++) {
                Cy = DosALa63;
                indexNew = Index[i];
                if (k%2 == 0) {                    /* Subtract */
                  for (j=indexL; j<indexH; j++) {
                    Result = Cy + Part[indexNew] - Part[j];
                    Cy = (Result > Part[indexNew] || (Result == Part[indexNew] && Cy != DosALa63)? DosALa63-1:DosALa63);
                    Part[indexNew] = Result;
                    indexNew++;
                    }
                  while (indexNew < Tmp) {
                    Result = Cy + Part[indexNew] + DosALa63;
                    Cy = (Result > Part[indexNew] || (Result == Part[indexNew] && Cy != DosALa63)? DosALa63-1:DosALa63);
                    Part[indexNew] = Result;
                    indexNew++;
                    }
                  }
                else {                              /* Add */
                  for (j=indexL; j<indexH; j++) {
                    Result = Cy + Part[indexNew] + Part[j];
                    Cy = (Result < Part[indexNew] || Result < Part[j]?
                         DosALa63+1: DosALa63);
                    Part[indexNew] = Result;
                    indexNew++;
                    }   
                  while (indexNew < Tmp) {
                    Result = Cy + Part[indexNew] + DosALa63;
                    Cy = (Result < Part[indexNew]?DosALa63+1: DosALa63);
                    Part[indexNew] = Result;
                    indexNew++;
                    }
                  }
                if (u==0) {
                  indexL = Index[i-(3*k+1)*k/2];
                  indexH = Index[i-(3*k+1)*k/2+1];
                  }
                }              // end for u
              }
            if (Part[Tmp - 1] == DosALa63) {
              len--;
              Tmp--;
              }
            else {
              }
            Index[i+1] = Tmp;
            }                      // end for i
          count = len*8;
          Conv = new byte[count+1];
          for (i=Index[val]; i<Tmp; i++) {
            Conv[count] = (byte)(Part[i] & 0xFF);
            Conv[count-1] = (byte)(Part[i] >>> 8 & 0xFF);
            Conv[count-2] = (byte)(Part[i] >>> 16 & 0xFF);
            Conv[count-3] = (byte)(Part[i] >>> 24 & 0xFF);
            Conv[count-4] = (byte)(Part[i] >>> 32 & 0xFF);
            Conv[count-5] = (byte)(Part[i] >>> 40 & 0xFF);
            Conv[count-6] = (byte)(Part[i] >>> 48 & 0xFF);
            Conv[count-7] = (byte)((Part[i] >>> 56 & 0xFF) ^ 0x80);
            count -= 8;
            }
          Conv[0] = 0;
          stackValues[stackIndex] = new BigInteger(Conv);
          break;
        case 'B':
        case 'N':
          int Base, Q, baseNbr;
          BigInteger value;
          if (stackOper == 'B') {
            j = stackValues[stackIndex+1].compareTo(BigInt3);
            if (j < 0) {return -8;}
            if (j == 0) {
              stackValues[stackIndex] = BigInt2;
              return 0;
              }
            value = stackValues[stackIndex+1].subtract(BigInt2).or(BigInt1);
            }
          else {
            if (stackValues[stackIndex+1].compareTo(BigInt2) < 0) {return -8;}
            value = stackValues[stackIndex+1].add(BigInt1).or(BigInt1);
            }
outer_calculate_SPRP:
          while (true) {        /* Search for next pseudoprime */
calculate_SPRP:
            do {
              if (value.bitLength() < 16) {
                j = value.intValue();
                if (j >= 9) {
                  for (Q=3; Q*Q<=j; Q+=2) {     /* Check if Base is prime */
                    if (j%Q == 0) {
                      break calculate_SPRP;     /* Composite */
                      }  
                    }
                  }
                break outer_calculate_SPRP;     /* Prime */
                }
              for (baseNbr=100; baseNbr>0; baseNbr--) {
                Base = 3;
                if (value.mod(BigInteger.valueOf(Base)).signum() == 0) {
                  break calculate_SPRP;         /* Composite */
                  }
calculate_new_prime3:
                do {
                  Base+=2;
                  for (Q=3; Q*Q<=Base; Q+=2) {  /* Check if Base is prime */
                    if (Base%Q == 0) {
                      continue calculate_new_prime3;   /* Composite */
                      }  
                    }
                    break;                      /* Prime found */
                  } while (true);
                if (value.mod(BigInteger.valueOf(Base)).signum() == 0) {
                    break calculate_SPRP;       /* Composite */
                  }
                }
              BigInteger valuem1 = value.subtract(BigInt1);
              int exp = valuem1.getLowestSetBit();
compute_SPRP_loop:
              for (baseNbr=20; baseNbr>0; baseNbr--) {
                Base = 3;
calculate_new_prime4:
                do {
                  Base+=2;
                  for (Q=3; Q*Q<=Base; Q+=2) {  /* Check if Base is prime */
                    if (Base%Q == 0) {
                      continue calculate_new_prime4;   /* Composite */
                      }  
                    }
                    break;                      /* Prime found */
                  } while (true);
                BigInteger bBase = BigInteger.valueOf(Base);
                BigInteger pow = bBase.modPow(valuem1.shiftRight(exp), value);
                if (pow.equals(BigInt1) || pow.equals(valuem1)) {
                  continue;                     /* Strong pseudoprime */
                  }
                for (j=1; j<exp; j++) {
                  pow = pow.multiply(pow).mod(value);
                  if (pow.equals(valuem1)) {
                    continue compute_SPRP_loop; /* Strong pseudoprime */
                    }
                  if (pow.equals(BigInt1)) {
                    break compute_SPRP_loop;    /* Composite */
                    }
                  }
                break;                          /* Composite */
                }                               /* End for */
              if (baseNbr == 0) {
                break outer_calculate_SPRP;     /* Strong pseudoprime */
                }
              } while (false);
            if (stackOper == 'B') {
              value = value.subtract(BigInt2);
              }
            else {
              value = value.add(BigInt2);
              }
            }
          stackValues[stackIndex] = value;
        }              /* end switch */
     return 0;
     }
  }
