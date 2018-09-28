package VCS::Interceptor;
use strict;

use VCS::Config;
use LWP;
use HTTP::Request;
use Digest::MD5  qw(md5_hex);
use Data::Dumper;
use Encode qw(decode encode);

sub new
# //////////////////////////////////////////////////
{
	my ( $class, $pclass, $vars ) = @_;
	
	my $self = bless {}, $pclass;
	
	$self->{ 'VCS::Vars' } = $vars;
	
	return $self;
}

	my $serv = '127.0.0.1';
	

sub send_docpack
# //////////////////////////////////////////////////
{

	my ( $self, $docid, $ptype, $summ ) = @_;
	
	my $data = {};
	my $vars = $self->{ 'VCS::Vars' };
	my $docobj = VCS::Docs::docs->new('VCS::Docs::docs', $vars);
	my $error = $docobj->getDocData(\$data, $docid, 'individuals');
	
	my $services = temporary_docservices( $self, $data, $ptype, $summ );

	my $request = xml_create( $services->{ services }, $services->{ info } );
	
	my $resp = send_request( $request );
	
	return split( /:/, $resp );
}

sub xml_create
# //////////////////////////////////////////////////
{
	my ( $services, $info ) = @_;
	
	
	my $md5line = '';
	my $xml = '<Services>';
	
	for my $service ( keys %$services ) {
	
		$xml .= '<Service>';
	
		for my $field ( sort { $a cmp $b } keys %{ $services->{ $service } } ) {
		
			$xml .= "<$field>" . $services->{ $service }->{ $field } . "</$field>";
			
			$md5line .= $services->{ $service }->{ $field };
		}
		
		$xml .= '</Service>';
	}
	
	$xml .= '</Services>';
	
	$xml .= '<Info>';
	
	for ( sort { $a cmp $b } keys %$info ) {
	
		$xml .= "<$_>" . $info->{ $_ } . "</$_>";
		
		$md5line .= $info->{ $_ };
	}
	
	$xml .= '</Info>';
	
	my $currentDate = get_date();
	
	$md5line .= $currentDate;
	
	Encode::from_to( $md5line, 'utf-8', 'windows-1251' );
	
	my $bytecode = "";
	
	$bytecode .= ord($_) . " " for split( //, $md5line );
	
	my $md5 = Digest::MD5->new->add( $bytecode )->hexdigest;
	
	$xml =  '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			$xml .
			'<Control>' .
				'<CRC>' . $md5 . '</CRC>' . 
				'<Date>' . $currentDate . '</Date>' .
			'</Control>' .
		'</toCashbox>';

	return $xml;
}

sub send_request
# //////////////////////////////////////////////////
{
	my $line = shift;
	
	my $ua = LWP::UserAgent->new;
	
	$ua->agent('Mozilla/4.0 (compatible; MSIE 6.0; X11; Linux i686; en) Opera 7.60');
	
	my $request = HTTP::Request->new(GET => $serv.'/?message='.$line.';');

	my $response = $ua->request($request);

	return "ERROR:нет связи с перехватчиком" if $response->{ _rc } != 200;
	
	return $response->{ _content };
}

sub get_date
# //////////////////////////////////////////////////
{
	my ( $sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst ) = localtime( time );
	
	$year += 1900;
	$mon++;
	
	for ( $sec, $min, $hour, $mday, $mon, $year ) { 
	
		$_ = '0'.$_ if $_ < 10;
	};
	
	return "$year-$mon-$mday $hour:$min:$sec";
}

sub temporary_docservices
# //////////////////////////////////////////////////
{
	my ( $self, $data, $ptype, $summ ) = @_;

	my $vars = $self->{'VCS::Vars'};
	my $gconfig = $vars->getConfig('general');

	my $mobile_kostyl = 0;
	my $kostyle_hash = {};

	my ($k_agr, $k_code, $k_name, $k_pers, $k_price, $k_prsum, $k_agrsum, $k_agrvat) = $vars->db->sel1("
		SELECT AgreementNo, Code, Name, PersNum, Price, PriceSum, AgrSum, AgrVAT
		FROM MobilJurDoc WHERE AgreementNo = ?", $data->{docnum} );
	
	if ( $k_agr ) { 
		$kostyle_hash->{code} = $k_code;
		$kostyle_hash->{num} = $k_pers;
		$kostyle_hash->{price} = $k_price;
		$kostyle_hash->{price_sum} = $k_prsum;
		$kostyle_hash->{sum} = $k_agrsum;
		$kostyle_hash->{nds} = $k_agrvat;
		$kostyle_hash->{name} = $k_name;
		
		$mobile_kostyl = 1;
	}
	
	my ( $cntres, $cntnres, $cntncon, $cntage, $smscnt, $shcnt, $shrows, $shind, $indexes, $dhlsum ) = ( 0, 0, 0, 0, 0, 0, {}, '', {}, 0 );

	if ( $data->{ shipping }==1 ) {
		$dhlsum = $data->{'shipsum'};
		$shcnt = 1;
	}

	my ( $apcnt, $astr, $bankid, $prevbank ) = ( 0, '', '', 0 );
	
	for my $ak ( @{ $data->{ applicants } } ) {

		next if $ak->{ Status } == 7;

		$apcnt++;
		
		if ( $data->{ jurid } ) {
		
			if ( $prevbank != $ak->{ InfoID } ) {
			
				$prevbank = $ak->{ InfoID };
				$cntncon += $ak->{ Num_NC } + $ak->{ Num_NN }; 
				$cntnres += $ak->{ Num_NR } - $ak->{ Num_NN }; 
				$cntres += $ak->{ VisaCnt } - $ak->{ Num_NR } - $ak->{ Num_NC } - $ak->{ Num_ACon };
				$cntage += $ak->{ Num_ACon };
			}
		} else {
			if ($ak->{ Concil }) {
				$cntncon++;
			} else {
				if ($ak->{ AgeCatA }) {
					$cntage++;
				} else {
					if ($ak->{ iNRes }) {
						$cntnres++;
					} else {					
						$cntres++;
					}
				}
			}
		}

		if ( ( $data->{'sms_status'} == 2) && ($ak->{'MobileNums'} ne '')) {
			$smscnt++;
		}
		if (($data->{'shipping'} == 2) && ($ak->{'ShipAddress'} ne '')) {
			$shcnt++;
			$dhlsum += $ak->{'RTShipSum'};

		}
	}
	
	my $rate = $data->{'rate'};
	my ($agesfree,$ages) = $vars->db->sel1('SELECT AgesFree,Ages FROM PriceRate WHERE ID=?',$rate);		
	my $prices = $vars->admfunc->getPrices($vars,$rate,$data->{'vtype'},$data->{'ipdate'});

	my $shsum = 0;
	if ($data->{'newdhl'}) {
		$shsum = sprintf("%.2f",$dhlsum);
	} else {
		$shsum = sprintf("%.2f",$prices->{'shipping'} * $shcnt);
	}
	if ($data->{'sms_status'} == 1) {
		$smscnt = 1;
	}
	
	my $vprice = ($data->{'urgent'} ? $prices->{($data->{'jurid'} ? 'j' : '').'urgent'} : $prices->{($data->{'jurid'} ? 'j' : '').'visa'});
		
	    #    1 - сервисный сбор (92101) киев
            #    2 - срочн.сервисный (92111) киев
            # 3 - ВИП комфорт
            # ок 4 - ВИП обслуживание
            # 5 - Доставка
            # 6 - КС	----------------> без НДС
            # 7 - СМС
            # 8 - ВИП Стандарт
            # ок 9 - сервисный сбор (00101) мск
            # ок 10 - срочн.сервисный (00111) мск
            # 11 - страхование --------------> без НДС
            # ок 12 - доставка
            # ок 13 - анкета
            # ок 14 - распечатка
            # ок 15 - фотопечать
            # ок 16 - ксерокс
	    
	    # страховка!!
	    # services!!
	
	my $servsums = {
		shipping => {
			Name		=> 'доставка',
			ServiceID	=> 5,
			Quantity	=> $shcnt,
			Price		=> sprintf( "%.2f", $prices->{ shipping } ),
			VAT		=> 1,
		},
		sms => {
			Name		=> 'смс',
			ServiceID	=> 7,
			Quantity	=> $smscnt,
			Price		=> sprintf( "%.2f", $prices->{ sms } ),
			VAT		=> 1,
		},
		tran => {
			Name		=> 'перевод',
			ServiceID	=> 17,
			Quantity	=> $data->{ transum },
			Price		=> sprintf( "%.2f", $prices->{ tran } ),
			VAT		=> 1,
		},
		xerox => {
			Name		=> 'ксерокс',
			ServiceID	=> 16,
			Quantity	=> $data->{ xerox },
			Price		=> sprintf( "%.2f", $prices->{ xerox } ),
			VAT		=> 1,
		},
		visa => {
			Name		=> ( $data->{'urgent'} ? 'срочн.' : '' ).'сервисный',
			ServiceID	=> ( $data->{'urgent'} ? 10 : 9 ),
			Quantity	=> $apcnt,
			Price		=> sprintf( "%.2f", $vprice ),
			VAT		=> 1,
		},
		ank => {
			Name		=> 'анкета',
			ServiceID	=> 13,
			Quantity	=> $data->{ anketasrv },
			Price		=> sprintf( "%.2f", $prices->{ anketasrv } ),
			VAT		=> 1,
		},
		print => {
			Name		=> 'печать',
			ServiceID	=> 14,
			Quantity	=> $data->{ printsrv },
			Price		=> sprintf( "%.2f", $prices->{ printsrv } ),
			VAT		=> 1,
		},
		photo => {
			Name		=> 'фото',
			ServiceID	=> 15,
			Quantity	=> $data->{ photosrv },
			Price		=> sprintf( "%.2f", $prices->{ photosrv } ),
			VAT		=> 1,
		},
		vip => {
			Name		=> 'vip',
			ServiceID	=> 4,
			Quantity	=> $data->{ vipsrv },
			Price		=> sprintf( "%.2f", $prices->{ vipsrv } ),
			VAT		=> 1,
		}
	};

	if ( $data->{'vcat'} eq 'D' ) {
	
		$servsums->{ cons }->{ pconsd } = sprintf('%.2f',$prices->{'concilr' . ($data->{'urgent'}?'u':'') });
		$servsums->{ cons }->{ cconsd } = $cntres;
		$servsums->{ cons }->{ sconsd } = sprintf('%.2f', $cntres * $servsums->{'pconsd'});
		
		$servsums->{ cons }->{ cconsc } = 0;
		$servsums->{ cons }->{ cconsr } = 0;
		
		$servsums->{ cons }->{ pconsc } = '0.00';
		$servsums->{ cons }->{ pconsr } = '0.00';
		$servsums->{ cons }->{ sconsc } = '0.00';
		$servsums->{ cons }->{ sconsr } = '0.00';
	}
	elsif ( $data->{ vcat } eq 'C' ) {
	
		$servsums->{ cons }->{ pconsc } = sprintf( '%.2f', $prices->{ 'concilr' . ( $data->{ urgent } ? 'u' : '' ) } );
		$servsums->{ cons }->{ pconsr } = sprintf( '%.2f', $prices->{ 'conciln' . ( $data->{ urgent } ? 'u' : '' ) } );
		$servsums->{ cons }->{ pconsa } = sprintf( '%.2f', $prices->{ 'concilr' . ( $data->{ urgent } ? 'u' : '' ) . '_' . $ages } );
		
		$servsums->{ cons }->{ cconsc } = $cntres;
		$servsums->{ cons }->{ cconsr } = $cntnres;
		$servsums->{ cons }->{ cconsa } = $cntage;
		
		$servsums->{ cons }->{ sconsc } = sprintf( '%.2f', $cntres * $servsums->{ pconsc } );
		$servsums->{ cons }->{ sconsr } = sprintf( '%.2f', $cntnres * $servsums->{ pconsr } );
		$servsums->{ cons }->{ sconsa } = sprintf( '%.2f', $cntage * $servsums->{ pconsa } );
		
		$servsums->{ cons }->{ cconsd } = 0;
		$servsums->{ cons }->{ pconsd } = '0.00';
		$servsums->{ cons }->{ sconsd } = '0.00';
	}
	# $servsums->{ consabs } = $cntncon;
	
	my $vip_type = {
		2 => 'vipprime',
		3 => 'vipprimespb',
		4 => 'vipcomfort',
	};
	
	my $serv_adds =  $vars->db->selallkeys("
		SELECT ServiceID, Price FROM ServicesPriceRates WHERE PriceRateID = ?", $rate);

	for ( @$serv_adds ) {
		next unless exists $vip_type->{ $_->{ ServiceID } };
		
		my $vip_tag = $vip_type->{ $_->{ ServiceID } };
		$data->{ $vip_tag.'prc' } = sprintf("%.2f", $_->{ Price } );
		$data->{ $vip_tag.'vat' } = sprintf("%.2f", $data->{ $vip_tag.'prc' } * $gconfig->{'VAT'} / (100 + $gconfig->{'VAT'}) );
	}
	
	if ( $mobile_kostyl ) {
		$servsums->{'visaprc'} = $kostyle_hash->{price};
		$servsums->{'visasum'} = $kostyle_hash->{price_sum};
	}
	
	for my $serv ( keys %$servsums ) {
	
		delete $servsums->{ $serv } unless $servsums->{ $serv }->{ Quantity };
	}
	
warn "------------------------";
warn Dumper($servsums);

	my $info = {
		AgrNumber => $data->{ docnum },
		Cashier => 'Иванов И.И.',
		CashierPass => 26,
		
		MoneyType => $ptype, # 1 - наличка, 2 - карта
		Total => '169.00',
		Money => $summ,
	};
	
	return { services => $servsums, info => $info };
}

1;
